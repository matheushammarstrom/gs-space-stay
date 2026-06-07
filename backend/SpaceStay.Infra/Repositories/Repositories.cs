using Microsoft.EntityFrameworkCore;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Domain;
using SpaceStay.Infra.Data;

namespace SpaceStay.Infra.Repositories;

// Implementações EF Core da camada de repositório. Todas compartilham o AppDbContext
// (scoped), então SaveChangesAsync persiste tudo que foi rastreado.

public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected readonly AppDbContext Db = db;
    protected DbSet<T> Set => Db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(object id) => await Set.FindAsync(id);
    public virtual async Task<List<T>> ListAllAsync() => await Set.AsNoTracking().ToListAsync();
    public virtual async Task AddAsync(T entity) => await Set.AddAsync(entity);
    public virtual void Update(T entity) => Set.Update(entity);
    public virtual void Remove(T entity) => Set.Remove(entity);
    public Task<int> SaveChangesAsync() => Db.SaveChangesAsync();
}

public class GuestRepository(AppDbContext db) : Repository<Guest>(db), IGuestRepository
{
    public Task<Guest?> GetByEmailAsync(string email) =>
        Db.Guests.FirstOrDefaultAsync(g => g.Email == email);

    public Task<bool> EmailExistsAsync(string email) =>
        Db.Guests.AnyAsync(g => g.Email == email);
}

public class StaffRepository(AppDbContext db) : Repository<Staff>(db), IStaffRepository
{
    public Task<Staff?> GetByEmailAsync(string email) =>
        Db.Staff.FirstOrDefaultAsync(s => s.Email == email);
}

public class ModuleRepository(AppDbContext db) : Repository<Module>(db), IModuleRepository
{
    public async Task<(List<Module> Items, int Total)> GetPagedAsync(int skip, int take)
    {
        var q = Db.Modules.AsNoTracking().OrderBy(m => m.Id);
        var total = await q.CountAsync();
        var items = await q.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public Task<int> CountActiveBookingsAsync(int moduleId) =>
        Db.Bookings.CountAsync(b => b.ModuleId == moduleId && b.Status == BookingStatus.active);
}

public class SensorRepository(AppDbContext db) : Repository<Sensor>(db), ISensorRepository
{
    public Task<List<Sensor>> GetByModuleAsync(int moduleId) =>
        Db.Sensors.AsNoTracking().Where(s => s.ModuleId == moduleId).OrderBy(s => s.Type).ToListAsync();

    public Task<Sensor?> GetByIdWithModuleAsync(int id) =>
        Db.Sensors.Include(s => s.Module).FirstOrDefaultAsync(s => s.Id == id);
}

public class SensorReadingRepository(AppDbContext db) : Repository<SensorReading>(db), ISensorReadingRepository
{
    public async Task<List<SensorReading>> GetLatestPerSensorForModuleAsync(int moduleId)
    {
        var sensorIds = Db.Sensors.Where(s => s.ModuleId == moduleId).Select(s => s.Id);

        // Pega o maior recorded_at de cada sensor e junta de volta para obter a leitura.
        var maxPerSensor = Db.SensorReadings
            .Where(r => sensorIds.Contains(r.SensorId))
            .GroupBy(r => r.SensorId)
            .Select(g => new { SensorId = g.Key, MaxAt = g.Max(r => r.RecordedAt) });

        var query =
            from r in Db.SensorReadings.AsNoTracking()
            join m in maxPerSensor
                on new { r.SensorId, r.RecordedAt } equals new { m.SensorId, RecordedAt = m.MaxAt }
            select r;

        return await query.ToListAsync();
    }
}

public class BookingRepository(AppDbContext db) : Repository<Booking>(db), IBookingRepository
{
    public async Task<(List<Booking> Items, int Total)> GetPagedAsync(int? guestId, int skip, int take)
    {
        var q = Db.Bookings.AsNoTracking().Include(b => b.Guest).Include(b => b.Module).AsQueryable();
        if (guestId.HasValue) q = q.Where(b => b.GuestId == guestId.Value);
        q = q.OrderByDescending(b => b.Id);

        var total = await q.CountAsync();
        var items = await q.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public Task<Booking?> GetByIdWithRefsAsync(int id) =>
        Db.Bookings.Include(b => b.Guest).Include(b => b.Module).FirstOrDefaultAsync(b => b.Id == id);

    public Task<int> CountOccupyingByModuleAsync(int moduleId, int? excludeBookingId = null) =>
        Db.Bookings.CountAsync(b => b.ModuleId == moduleId
            && (b.Status == BookingStatus.active || b.Status == BookingStatus.confirmed)
            && (excludeBookingId == null || b.Id != excludeBookingId));
}

public class AlertRepository(AppDbContext db) : Repository<Alert>(db), IAlertRepository
{
    public async Task<(List<Alert> Items, int Total)> GetPagedAsync(AlertStatus? status, int skip, int take)
    {
        var q = Db.Alerts.AsNoTracking()
            .Include(a => a.Module).Include(a => a.Sensor).Include(a => a.ResolvedByStaff)
            .AsQueryable();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        q = q.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id);

        var total = await q.CountAsync();
        var items = await q.Skip(skip).Take(take).ToListAsync();
        return (items, total);
    }

    public Task<Alert?> GetByIdWithRefsAsync(int id) =>
        Db.Alerts.Include(a => a.Module).Include(a => a.Sensor).Include(a => a.ResolvedByStaff)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Dictionary<int, int>> CountOpenByModuleAsync(IReadOnlyCollection<int> moduleIds)
    {
        if (moduleIds.Count == 0) return new Dictionary<int, int>();
        return await Db.Alerts
            .Where(a => a.Status == AlertStatus.open && moduleIds.Contains(a.ModuleId))
            .GroupBy(a => a.ModuleId)
            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ModuleId, x => x.Count);
    }
}

public class ExcursionRepository(AppDbContext db) : Repository<Excursion>(db), IExcursionRepository
{
    public Task<int> CountActiveBookingsAsync(int excursionId) =>
        Db.ExcursionBookings.CountAsync(eb =>
            eb.ExcursionId == excursionId && eb.Status != ExcursionBookingStatus.cancelled);
}

public class ExcursionBookingRepository(AppDbContext db) : Repository<ExcursionBooking>(db), IExcursionBookingRepository
{
    // Verifica qualquer reserva existente (a UNIQUE (guest_id, excursion_id) também impede duplicata).
    public Task<bool> ExistsAsync(int guestId, int excursionId) =>
        Db.ExcursionBookings.AnyAsync(eb => eb.GuestId == guestId && eb.ExcursionId == excursionId);

    public Task<List<ExcursionBooking>> GetByGuestAsync(int guestId) =>
        Db.ExcursionBookings.AsNoTracking().Include(eb => eb.Excursion)
            .Where(eb => eb.GuestId == guestId).ToListAsync();

    // Rastreado (sem AsNoTracking) para permitir o Remove no cancelamento.
    public Task<ExcursionBooking?> GetAsync(int guestId, int excursionId) =>
        Db.ExcursionBookings.FirstOrDefaultAsync(eb => eb.GuestId == guestId && eb.ExcursionId == excursionId);
}
