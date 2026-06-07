using SpaceStay.Core.Domain;

namespace SpaceStay.Core.Abstractions;

// Camada de repositório: acesso a dados, sem regra de negócio. As implementações
// (EF Core) ficam em Infra/Repositories e compartilham o DbContext da requisição,
// então um SaveChangesAsync grava todas as mudanças rastreadas.

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<List<T>> ListAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}

public interface IGuestRepository : IRepository<Guest>
{
    Task<Guest?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}

public interface IStaffRepository : IRepository<Staff>
{
    Task<Staff?> GetByEmailAsync(string email);
}

public interface IModuleRepository : IRepository<Module>
{
    Task<(List<Module> Items, int Total)> GetPagedAsync(int skip, int take);
    Task<int> CountActiveBookingsAsync(int moduleId);
}

public interface ISensorRepository : IRepository<Sensor>
{
    Task<List<Sensor>> GetByModuleAsync(int moduleId);
    Task<Sensor?> GetByIdWithModuleAsync(int id);
}

public interface ISensorReadingRepository : IRepository<SensorReading>
{
    Task<List<SensorReading>> GetLatestPerSensorForModuleAsync(int moduleId);
}

public interface IBookingRepository : IRepository<Booking>
{
    Task<(List<Booking> Items, int Total)> GetPagedAsync(int? guestId, int skip, int take);
    Task<Booking?> GetByIdWithRefsAsync(int id);
    // Reservas que ocupam o módulo (active/confirmed); excludeBookingId ignora uma reserva.
    Task<int> CountOccupyingByModuleAsync(int moduleId, int? excludeBookingId = null);
}

public interface IAlertRepository : IRepository<Alert>
{
    Task<(List<Alert> Items, int Total)> GetPagedAsync(AlertStatus? status, int skip, int take);
    Task<Alert?> GetByIdWithRefsAsync(int id);
    Task<Dictionary<int, int>> CountOpenByModuleAsync(IReadOnlyCollection<int> moduleIds);
}

public interface IExcursionRepository : IRepository<Excursion>
{
    Task<int> CountActiveBookingsAsync(int excursionId);
}

public interface IExcursionBookingRepository : IRepository<ExcursionBooking>
{
    Task<bool> ExistsAsync(int guestId, int excursionId);
    Task<List<ExcursionBooking>> GetByGuestAsync(int guestId);
    Task<ExcursionBooking?> GetAsync(int guestId, int excursionId);
}
