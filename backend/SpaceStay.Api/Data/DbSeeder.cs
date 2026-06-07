using Microsoft.EntityFrameworkCore;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Domain;
using SpaceStay.Infra.Data;

namespace SpaceStay.Api.Data;

// Popula o banco com um cenário de demonstração (espelha o database/seed.sql, mas com
// senhas reais geradas pelo nosso hasher, para o login funcionar). Só roda com o banco
// vazio. As credenciais de teste estão no README do backend.
public static class DbSeeder
{
    public const string GuestPassword = "Guest@123";
    public const string AdminPassword = "Admin@123";
    public const string EngineerPassword = "Engineer@123";
    public const string ConciergePassword = "Concierge@123";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        if (await db.Modules.AnyAsync()) return; // já populado

        var now = DateTime.UtcNow;

        // equipe (senhas reais para login)
        var helena = new Staff { Name = "Helena Vasquez", Email = "helena.vasquez@spacestay.space", Role = StaffRole.admin, PasswordHash = hasher.Hash(AdminPassword), CreatedAt = now };
        var rafael = new Staff { Name = "Rafael Lima", Email = "rafael.lima@spacestay.space", Role = StaffRole.engineer, PasswordHash = hasher.Hash(EngineerPassword), CreatedAt = now };
        var sofiaM = new Staff { Name = "Sofia Mendes", Email = "sofia.mendes@spacestay.space", Role = StaffRole.concierge, PasswordHash = hasher.Hash(ConciergePassword), CreatedAt = now };
        db.Staff.AddRange(helena, rafael, sofiaM);

        // hóspedes (senha = Guest@123)
        var ana = new Guest { Name = "Ana Beatriz Costa", Email = "ana.costa@example.com", Nationality = "Brasil", MedicalClearance = true, PasswordHash = hasher.Hash(GuestPassword), CreatedAt = now };
        var kenji = new Guest { Name = "Kenji Tanaka", Email = "kenji.tanaka@example.com", Nationality = "Japão", MedicalClearance = true, PasswordHash = hasher.Hash(GuestPassword), CreatedAt = now };
        var liam = new Guest { Name = "Liam O'Connor", Email = "liam.oconnor@example.com", Nationality = "Irlanda", MedicalClearance = true, PasswordHash = hasher.Hash(GuestPassword), CreatedAt = now };
        db.Guests.AddRange(ana, kenji, liam);

        // módulos
        var cupola = new Module { Name = "Cupola Suite 3", Type = ModuleType.suite, Capacity = 2, Status = ModuleStatus.occupied };
        var aurora = new Module { Name = "Aurora Suite 1", Type = ModuleType.suite, Capacity = 2, Status = ModuleStatus.available };
        db.Modules.AddRange(cupola, aurora);
        await db.SaveChangesAsync();

        // sensores (6 por módulo, com as faixas seguras de referência)
        var allSensors = new[] { cupola, aurora }.SelectMany(SensorsFor).ToList();
        db.Sensors.AddRange(allSensors);
        await db.SaveChangesAsync();

        // leituras (todas dentro da faixa; o pico de CO₂ é disparado ao vivo na demo)
        SensorReading R(Module m, SensorType t, decimal v, int minsAgo)
        {
            var s = allSensors.First(x => x.ModuleId == m.Id && x.Type == t);
            return new SensorReading { SensorId = s.Id, Value = v, RecordedAt = now.AddMinutes(-minsAgo) };
        }
        db.SensorReadings.AddRange(
            R(cupola, SensorType.o2, 20.9m, 60), R(cupola, SensorType.o2, 20.8m, 0),
            R(cupola, SensorType.co2, 620m, 60), R(cupola, SensorType.co2, 780m, 0),
            R(cupola, SensorType.pressure, 101.3m, 0),
            R(cupola, SensorType.temperature, 22.4m, 0),
            R(cupola, SensorType.humidity, 46m, 0),
            R(cupola, SensorType.water, 74m, 0),
            R(aurora, SensorType.o2, 20.7m, 0),
            R(aurora, SensorType.co2, 560m, 0),
            R(aurora, SensorType.pressure, 100.8m, 0),
            R(aurora, SensorType.temperature, 21.8m, 0),
            R(aurora, SensorType.humidity, 53m, 0),
            R(aurora, SensorType.water, 68m, 0)
        );

        // reservas: Cupola lotada (Ana e Liam ativos) para testar 409; Aurora fica livre para testar criação
        db.Bookings.AddRange(
            new Booking { GuestId = ana.Id, ModuleId = cupola.Id, CheckIn = now.AddDays(-3), CheckOut = now.AddDays(4), Status = BookingStatus.active },
            new Booking { GuestId = liam.Id, ModuleId = cupola.Id, CheckIn = now.AddDays(-1), CheckOut = now.AddDays(3), Status = BookingStatus.active }
        );

        // excursões
        var ex1 = new Excursion { Name = "Caminhada Espacial (EVA)", Description = "Atividade extraveicular guiada com vista direta da órbita.", Capacity = 4, ScheduledAt = now.AddDays(6) };
        var ex2 = new Excursion { Name = "Observação da Terra na Cúpula", Description = "Sessão contemplativa na cúpula panorâmica.", Capacity = 8, ScheduledAt = now.AddDays(3) };
        var ex3 = new Excursion { Name = "Jantar em Gravidade Zero", Description = "Experiência gastronômica em microgravidade.", Capacity = 12, ScheduledAt = now.AddDays(5) };
        db.Excursions.AddRange(ex1, ex2, ex3);
        await db.SaveChangesAsync();

        db.ExcursionBookings.AddRange(
            new ExcursionBooking { GuestId = ana.Id, ExcursionId = ex1.Id, Status = ExcursionBookingStatus.booked },
            new ExcursionBooking { GuestId = ana.Id, ExcursionId = ex2.Id, Status = ExcursionBookingStatus.booked }
        );
        await db.SaveChangesAsync();
    }

    private static IEnumerable<Sensor> SensorsFor(Module m) => new[]
    {
        new Sensor { ModuleId = m.Id, Type = SensorType.o2,          Unit = "%",   MinSafe = 19.5m, MaxSafe = 23.5m },
        new Sensor { ModuleId = m.Id, Type = SensorType.co2,         Unit = "ppm", MinSafe = 0m,    MaxSafe = 1000m },
        new Sensor { ModuleId = m.Id, Type = SensorType.pressure,    Unit = "kPa", MinSafe = 95m,   MaxSafe = 105m },
        new Sensor { ModuleId = m.Id, Type = SensorType.temperature, Unit = "°C",  MinSafe = 18m,   MaxSafe = 27m },
        new Sensor { ModuleId = m.Id, Type = SensorType.humidity,    Unit = "%RH", MinSafe = 30m,   MaxSafe = 60m },
        new Sensor { ModuleId = m.Id, Type = SensorType.water,       Unit = "%",   MinSafe = 20m,   MaxSafe = 100m },
    };
}
