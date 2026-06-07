using System.ComponentModel.DataAnnotations.Schema;

namespace SpaceStay.Core.Domain;

// POCOs que espelham as 9 tabelas de database/schema.sql. O mapeamento fino (FKs,
// ENUMs, índices, ON DELETE) fica em Infra/Data/AppDbContext.cs.

[Table("guests")]
public class Guest
{
    [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("email")] public string Email { get; set; } = null!;
    [Column("password_hash")] public string PasswordHash { get; set; } = null!;
    [Column("nationality")] public string? Nationality { get; set; }
    [Column("medical_clearance")] public bool MedicalClearance { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<ExcursionBooking> ExcursionBookings { get; set; } = new List<ExcursionBooking>();
}

[Table("staff")]
public class Staff
{
    [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("email")] public string Email { get; set; } = null!;
    [Column("password_hash")] public string PasswordHash { get; set; } = null!;
    [Column("role")] public StaffRole Role { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }

    public ICollection<Alert> ResolvedAlerts { get; set; } = new List<Alert>();
}

[Table("modules")]
public class Module
{
    [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("type")] public ModuleType Type { get; set; }
    [Column("capacity")] public int Capacity { get; set; }
    [Column("status")] public ModuleStatus Status { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

[Table("bookings")]
public class Booking
{
    [Column("id")] public int Id { get; set; }
    [Column("guest_id")] public int GuestId { get; set; }
    [Column("module_id")] public int ModuleId { get; set; }
    [Column("check_in")] public DateTime CheckIn { get; set; }
    [Column("check_out")] public DateTime CheckOut { get; set; }
    [Column("status")] public BookingStatus Status { get; set; }

    public Guest? Guest { get; set; }
    public Module? Module { get; set; }
}

[Table("sensors")]
public class Sensor
{
    [Column("id")] public int Id { get; set; }
    [Column("module_id")] public int ModuleId { get; set; }
    [Column("type")] public SensorType Type { get; set; }
    [Column("unit")] public string Unit { get; set; } = null!;
    [Column("min_safe")] public decimal MinSafe { get; set; }
    [Column("max_safe")] public decimal MaxSafe { get; set; }

    public Module? Module { get; set; }
    public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

// PK long porque a tabela de leituras cresce em alto volume.
[Table("sensor_readings")]
public class SensorReading
{
    [Column("id")] public long Id { get; set; }
    [Column("sensor_id")] public int SensorId { get; set; }
    [Column("value")] public decimal Value { get; set; }
    [Column("recorded_at")] public DateTime RecordedAt { get; set; }

    public Sensor? Sensor { get; set; }
}

[Table("alerts")]
public class Alert
{
    [Column("id")] public int Id { get; set; }
    [Column("sensor_id")] public int SensorId { get; set; }
    [Column("module_id")] public int ModuleId { get; set; }
    [Column("severity")] public AlertSeverity Severity { get; set; }
    [Column("message")] public string Message { get; set; } = null!;
    [Column("status")] public AlertStatus Status { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("resolved_by")] public int? ResolvedBy { get; set; }

    public Sensor? Sensor { get; set; }
    public Module? Module { get; set; }
    public Staff? ResolvedByStaff { get; set; }
}

[Table("excursions")]
public class Excursion
{
    [Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = null!;
    [Column("description")] public string? Description { get; set; }
    [Column("capacity")] public int Capacity { get; set; }
    [Column("scheduled_at")] public DateTime ScheduledAt { get; set; }

    public ICollection<ExcursionBooking> ExcursionBookings { get; set; } = new List<ExcursionBooking>();
}

[Table("excursion_bookings")]
public class ExcursionBooking
{
    [Column("id")] public int Id { get; set; }
    [Column("guest_id")] public int GuestId { get; set; }
    [Column("excursion_id")] public int ExcursionId { get; set; }
    [Column("status")] public ExcursionBookingStatus Status { get; set; }

    public Guest? Guest { get; set; }
    public Excursion? Excursion { get; set; }
}
