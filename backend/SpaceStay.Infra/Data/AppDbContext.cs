using Microsoft.EntityFrameworkCore;
using SpaceStay.Core.Domain;

namespace SpaceStay.Infra.Data;

// DbContext do EF Core (Code-First). A configuração Fluent abaixo espelha o
// database/schema.sql (Parte 1): ENUM nativo do MySQL, tamanhos de VARCHAR, índices,
// UNIQUEs e o ON DELETE de cada FK.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Excursion> Excursions => Set<Excursion>();
    public DbSet<ExcursionBooking> ExcursionBookings => Set<ExcursionBooking>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Alinha todos os DateTime ao schema.sql (DATETIME, precisão 0). Sem isso o
        // Pomelo geraria datetime(6), e o DEFAULT CURRENT_TIMESTAMP daria erro no MySQL.
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("datetime");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasCharSet("utf8mb4");

        // guests
        b.Entity<Guest>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Email).HasMaxLength(180).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Nationality).HasMaxLength(60);
            e.Property(x => x.MedicalClearance).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Email).IsUnique();
        });

        // staff
        b.Entity<Staff>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Email).HasMaxLength(180).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role)
                .HasColumnType("enum('admin','engineer','concierge')")
                .HasConversion<string>().IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Email).IsUnique();
        });

        // modules
        b.Entity<Module>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Type)
                .HasColumnType("enum('suite','common','lab')")
                .HasConversion<string>().IsRequired();
            e.Property(x => x.Status)
                .HasColumnType("enum('available','occupied','maintenance')")
                .HasConversion<string>().HasDefaultValue(ModuleStatus.available).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("chk_modules_capacity", "capacity > 0"));
        });

        // bookings
        b.Entity<Booking>(e =>
        {
            e.Property(x => x.Status)
                .HasColumnType("enum('confirmed','active','completed')")
                .HasConversion<string>().HasDefaultValue(BookingStatus.confirmed).IsRequired();

            e.HasOne(x => x.Guest).WithMany(g => g.Bookings)
                .HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Module).WithMany(m => m.Bookings)
                .HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("chk_bookings_dates", "check_out > check_in"));
            e.HasIndex(x => new { x.ModuleId, x.Status });
            e.HasIndex(x => x.GuestId);
        });

        // sensors
        b.Entity<Sensor>(e =>
        {
            e.Property(x => x.Type)
                .HasColumnType("enum('o2','co2','pressure','temperature','humidity','water')")
                .HasConversion<string>().IsRequired();
            e.Property(x => x.Unit).HasMaxLength(12).IsRequired();
            e.Property(x => x.MinSafe).HasColumnType("decimal(8,2)");
            e.Property(x => x.MaxSafe).HasColumnType("decimal(8,2)");

            e.HasOne(x => x.Module).WithMany(m => m.Sensors)
                .HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);

            e.ToTable(t => t.HasCheckConstraint("chk_sensors_range", "max_safe > min_safe"));
            e.HasIndex(x => new { x.ModuleId, x.Type }).IsUnique();   // 1 sensor por tipo/módulo
        });

        // sensor_readings
        b.Entity<SensorReading>(e =>
        {
            e.Property(x => x.Value).HasColumnType("decimal(8,2)");
            e.Property(x => x.RecordedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Sensor).WithMany(s => s.Readings)
                .HasForeignKey(x => x.SensorId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.SensorId, x.RecordedAt });
        });

        // alerts
        b.Entity<Alert>(e =>
        {
            e.Property(x => x.Severity)
                .HasColumnType("enum('info','warning','critical')")
                .HasConversion<string>().IsRequired();
            e.Property(x => x.Message).HasMaxLength(255).IsRequired();
            e.Property(x => x.Status)
                .HasColumnType("enum('open','acknowledged','resolved')")
                .HasConversion<string>().HasDefaultValue(AlertStatus.open).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Sensor).WithMany(s => s.Alerts)
                .HasForeignKey(x => x.SensorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Module).WithMany(m => m.Alerts)
                .HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ResolvedByStaff).WithMany(s => s.ResolvedAlerts)
                .HasForeignKey(x => x.ResolvedBy).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ModuleId);
        });

        // excursions
        b.Entity<Excursion>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.ToTable(t => t.HasCheckConstraint("chk_excursions_capacity", "capacity > 0"));
        });

        // excursion_bookings
        b.Entity<ExcursionBooking>(e =>
        {
            e.Property(x => x.Status)
                .HasColumnType("enum('booked','cancelled','attended')")
                .HasConversion<string>().HasDefaultValue(ExcursionBookingStatus.booked).IsRequired();

            e.HasOne(x => x.Guest).WithMany(g => g.ExcursionBookings)
                .HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Excursion).WithMany(ex => ex.ExcursionBookings)
                .HasForeignKey(x => x.ExcursionId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.GuestId, x.ExcursionId }).IsUnique();  // sem reserva duplicada
        });
    }
}
