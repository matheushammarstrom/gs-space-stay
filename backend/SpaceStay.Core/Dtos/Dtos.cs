using System.ComponentModel.DataAnnotations;
using SpaceStay.Core.Domain;

namespace SpaceStay.Core.Dtos;

// DTOs de entrada e saída. Os de requisição usam data annotations para validação
// (Parte 5); as entidades do EF nunca são expostas diretamente.

// Auth
public class RegisterGuestRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [StringLength(60)]
    public string? Nationality { get; set; }

    public bool MedicalClearance { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public record AuthResponse(string Token, DateTime ExpiresAt, string UserType, string? Role, int Id, string Name);

// Módulos, sensores e leituras
public record ModuleResponse(int Id, string Name, ModuleType Type, int Capacity, ModuleStatus Status, int OpenAlerts);

public record SensorResponse(int Id, SensorType Type, string Unit, decimal MinSafe, decimal MaxSafe);

public record ReadingResponse(
    int SensorId, SensorType Type, string Unit, decimal? Value, DateTime? RecordedAt,
    decimal MinSafe, decimal MaxSafe, string Status, string Comfort);

// Reservas
public class CreateBookingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "moduleId inválido.")]
    public int ModuleId { get; set; }

    [Required] public DateTime CheckIn { get; set; }
    [Required] public DateTime CheckOut { get; set; }
}

public class UpdateBookingRequest
{
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public BookingStatus? Status { get; set; }
}

public record BookingResponse(
    int Id, int GuestId, string? GuestName, int ModuleId, string? ModuleName,
    DateTime CheckIn, DateTime CheckOut, BookingStatus Status);

// Alertas
public record AlertResponse(
    int Id, int SensorId, SensorType SensorType, int ModuleId, string? ModuleName,
    AlertSeverity Severity, string Message, AlertStatus Status, DateTime CreatedAt,
    int? ResolvedBy, string? ResolvedByName);

// Ingestão de leituras (ponte IoT da Parte 6)
public class CreateReadingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "sensorId inválido.")]
    public int SensorId { get; set; }

    [Required] public decimal Value { get; set; }

    // Opcional; quando ausente, usa o instante atual.
    public DateTime? RecordedAt { get; set; }
}

public record ReadingIngestResponse(long ReadingId, bool OutOfRange, bool AlertCreated, AlertResponse? Alert);

// Excursões
public record ExcursionResponse(
    int Id, string Name, string? Description, int Capacity, DateTime ScheduledAt,
    int BookedCount, int AvailableSpots, bool BookedByMe);

public record BookExcursionResponse(int ExcursionBookingId, int ExcursionId, int GuestId, ExcursionBookingStatus Status);
