using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Abstractions;

// Camada de serviço: regras de negócio (validações, regra de limiar de alerta, RBAC).
// Os controllers chamam estes serviços, que por sua vez usam os repositórios.

public interface IAuthService
{
    Task<AuthResponse> RegisterGuestAsync(RegisterGuestRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public interface IModuleService
{
    Task<PagedResult<ModuleResponse>> GetModulesAsync(PageRequest page);
    Task<ModuleResponse> GetModuleAsync(int id);
    Task<List<SensorResponse>> GetSensorsAsync(int moduleId);
    Task<List<ReadingResponse>> GetLatestReadingsAsync(int moduleId);
}

public interface IBookingService
{
    Task<PagedResult<BookingResponse>> GetBookingsAsync(CurrentUser user, PageRequest page);
    Task<BookingResponse> GetByIdAsync(int id, CurrentUser user);
    Task<BookingResponse> CreateAsync(CreateBookingRequest request, CurrentUser user);
    Task<BookingResponse> UpdateAsync(int id, UpdateBookingRequest request, CurrentUser user);
    Task DeleteAsync(int id, CurrentUser user);
}

public interface IAlertService
{
    Task<PagedResult<AlertResponse>> GetAlertsAsync(AlertStatus? status, PageRequest page);
    Task<AlertResponse> AcknowledgeAsync(int id, CurrentUser staff);
}

public interface IReadingService
{
    // Persiste a leitura e, se estiver fora da faixa segura, cria um alerta.
    Task<ReadingIngestResponse> IngestAsync(CreateReadingRequest request);
}

public interface IExcursionService
{
    Task<List<ExcursionResponse>> GetExcursionsAsync(CurrentUser user);
    Task<BookExcursionResponse> BookAsync(int excursionId, CurrentUser user);
    Task<ExcursionResponse> CancelAsync(int excursionId, CurrentUser user);
}
