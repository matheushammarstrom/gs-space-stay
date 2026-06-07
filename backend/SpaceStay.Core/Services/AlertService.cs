using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class AlertService(IAlertRepository alerts) : IAlertService
{
    public async Task<PagedResult<AlertResponse>> GetAlertsAsync(AlertStatus? status, PageRequest page)
    {
        var (items, total) = await alerts.GetPagedAsync(status, page.Skip, page.PageSize);
        var mapped = items.Select(Map).ToList();
        return new PagedResult<AlertResponse>(mapped, page.Page, page.PageSize, total);
    }

    public async Task<AlertResponse> AcknowledgeAsync(int id, CurrentUser staff)
    {
        if (!staff.IsStaff)
            throw new ForbiddenException("Apenas a equipe pode reconhecer alertas.");

        var alert = await alerts.GetByIdWithRefsAsync(id) ?? throw new NotFoundException("Alerta não encontrado.");

        if (alert.Status == AlertStatus.resolved)
            throw new ConflictException("Alerta já resolvido.");

        alert.Status = AlertStatus.acknowledged;
        alert.ResolvedBy = staff.Id;           // registra quem tratou (resolved_by)
        alerts.Update(alert);
        await alerts.SaveChangesAsync();

        var refreshed = await alerts.GetByIdWithRefsAsync(id) ?? alert;
        return Map(refreshed);
    }

    private static AlertResponse Map(Alert a) => new(
        a.Id, a.SensorId, a.Sensor?.Type ?? default, a.ModuleId, a.Module?.Name,
        a.Severity, a.Message, a.Status, a.CreatedAt, a.ResolvedBy, a.ResolvedByStaff?.Name);
}
