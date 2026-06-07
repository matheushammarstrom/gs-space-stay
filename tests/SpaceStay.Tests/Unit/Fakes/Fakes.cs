using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Domain;

namespace SpaceStay.Tests.Unit.Fakes;

// Test doubles (fakes) em memória para os repositórios. Permitem testar a regra de
// negócio dos serviços de forma isolada, sem banco de dados.

public class FakeTokenService : ITokenService
{
    public TokenResult GenerateToken(int userId, string email, string userType, string? role)
        => new($"fake-token-{userType}-{userId}", DateTime.UtcNow.AddHours(1));
}

public class FakeGuestRepository : IGuestRepository
{
    public readonly List<Guest> Items = new();
    private int _nextId = 1;

    public Task<Guest?> GetByEmailAsync(string email)
        => Task.FromResult(Items.FirstOrDefault(g => g.Email == email));
    public Task<bool> EmailExistsAsync(string email)
        => Task.FromResult(Items.Any(g => g.Email == email));

    public Task<Guest?> GetByIdAsync(object id)
        => Task.FromResult(Items.FirstOrDefault(g => g.Id == (int)id));
    public Task AddAsync(Guest entity) { entity.Id = _nextId++; Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<List<Guest>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(Guest entity) { }
    public void Remove(Guest entity) => Items.Remove(entity);
}

public class FakeStaffRepository : IStaffRepository
{
    public readonly List<Staff> Items = new();

    public Task<Staff?> GetByEmailAsync(string email)
        => Task.FromResult(Items.FirstOrDefault(s => s.Email == email));

    public Task<Staff?> GetByIdAsync(object id)
        => Task.FromResult(Items.FirstOrDefault(s => s.Id == (int)id));
    public Task AddAsync(Staff entity) { Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<List<Staff>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(Staff entity) { }
    public void Remove(Staff entity) => Items.Remove(entity);
}

public class FakeModuleRepository : IModuleRepository
{
    public readonly List<Module> Items = new();

    public Task<Module?> GetByIdAsync(object id)
        => Task.FromResult(Items.FirstOrDefault(m => m.Id == (int)id));
    public void Update(Module entity) { }

    public Task<(List<Module> Items, int Total)> GetPagedAsync(int skip, int take)
        => Task.FromResult((Items.Skip(skip).Take(take).ToList(), Items.Count));
    public Task<int> CountActiveBookingsAsync(int moduleId) => Task.FromResult(0);

    public Task AddAsync(Module entity) { Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<List<Module>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Remove(Module entity) => Items.Remove(entity);
}

public class FakeBookingRepository : IBookingRepository
{
    public readonly List<Booking> Items = new();
    private int _nextId = 1;

    public Task<int> CountOccupyingByModuleAsync(int moduleId, int? excludeBookingId = null)
        => Task.FromResult(Items.Count(b => b.ModuleId == moduleId
            && (b.Status == BookingStatus.active || b.Status == BookingStatus.confirmed)
            && (excludeBookingId == null || b.Id != excludeBookingId)));

    public Task<Booking?> GetByIdWithRefsAsync(int id)
        => Task.FromResult(Items.FirstOrDefault(b => b.Id == id));

    public Task<(List<Booking> Items, int Total)> GetPagedAsync(int? guestId, int skip, int take)
    {
        var q = Items.AsEnumerable();
        if (guestId.HasValue) q = q.Where(b => b.GuestId == guestId.Value);
        var list = q.ToList();
        return Task.FromResult((list.Skip(skip).Take(take).ToList(), list.Count));
    }

    public Task AddAsync(Booking entity) { entity.Id = _nextId++; Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<Booking?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(b => b.Id == (int)id));
    public Task<List<Booking>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(Booking entity) { }
    public void Remove(Booking entity) => Items.Remove(entity);
}

public class FakeSensorRepository : ISensorRepository
{
    public readonly List<Sensor> Items = new();

    public Task<Sensor?> GetByIdWithModuleAsync(int id)
        => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<List<Sensor>> GetByModuleAsync(int moduleId)
        => Task.FromResult(Items.Where(s => s.ModuleId == moduleId).ToList());

    public Task<Sensor?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(s => s.Id == (int)id));
    public Task AddAsync(Sensor entity) { Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<List<Sensor>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(Sensor entity) { }
    public void Remove(Sensor entity) => Items.Remove(entity);
}

public class FakeSensorReadingRepository : ISensorReadingRepository
{
    public readonly List<SensorReading> Items = new();
    private long _nextId = 1;

    public Task<List<SensorReading>> GetLatestPerSensorForModuleAsync(int moduleId)
        => Task.FromResult(new List<SensorReading>());

    public Task AddAsync(SensorReading entity) { entity.Id = _nextId++; Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<SensorReading?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(r => r.Id == (long)id));
    public Task<List<SensorReading>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(SensorReading entity) { }
    public void Remove(SensorReading entity) => Items.Remove(entity);
}

public class FakeAlertRepository : IAlertRepository
{
    public readonly List<Alert> Items = new();
    private int _nextId = 1;

    public Task<(List<Alert> Items, int Total)> GetPagedAsync(AlertStatus? status, int skip, int take)
    {
        var q = Items.AsEnumerable();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        var list = q.ToList();
        return Task.FromResult((list.Skip(skip).Take(take).ToList(), list.Count));
    }
    public Task<Alert?> GetByIdWithRefsAsync(int id) => Task.FromResult(Items.FirstOrDefault(a => a.Id == id));
    public Task<Dictionary<int, int>> CountOpenByModuleAsync(IReadOnlyCollection<int> moduleIds)
        => Task.FromResult(new Dictionary<int, int>());

    public Task AddAsync(Alert entity) { entity.Id = _nextId++; Items.Add(entity); return Task.CompletedTask; }
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
    public Task<Alert?> GetByIdAsync(object id) => Task.FromResult(Items.FirstOrDefault(a => a.Id == (int)id));
    public Task<List<Alert>> ListAllAsync() => Task.FromResult(Items.ToList());
    public void Update(Alert entity) { }
    public void Remove(Alert entity) => Items.Remove(entity);
}
