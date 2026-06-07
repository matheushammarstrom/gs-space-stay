using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class ModuleService(
    IModuleRepository modules,
    ISensorRepository sensors,
    ISensorReadingRepository readings,
    IAlertRepository alerts) : IModuleService
{
    public async Task<PagedResult<ModuleResponse>> GetModulesAsync(PageRequest page)
    {
        var (items, total) = await modules.GetPagedAsync(page.Skip, page.PageSize);
        var openByModule = await alerts.CountOpenByModuleAsync(items.Select(m => m.Id).ToList());

        var mapped = items
            .Select(m => new ModuleResponse(m.Id, m.Name, m.Type, m.Capacity, m.Status,
                openByModule.TryGetValue(m.Id, out var n) ? n : 0))
            .ToList();

        return new PagedResult<ModuleResponse>(mapped, page.Page, page.PageSize, total);
    }

    public async Task<ModuleResponse> GetModuleAsync(int id)
    {
        var m = await modules.GetByIdAsync(id) ?? throw new NotFoundException("Módulo não encontrado.");
        var openByModule = await alerts.CountOpenByModuleAsync(new[] { id });
        return new ModuleResponse(m.Id, m.Name, m.Type, m.Capacity, m.Status,
            openByModule.TryGetValue(id, out var n) ? n : 0);
    }

    public async Task<List<SensorResponse>> GetSensorsAsync(int moduleId)
    {
        _ = await modules.GetByIdAsync(moduleId) ?? throw new NotFoundException("Módulo não encontrado.");
        var list = await sensors.GetByModuleAsync(moduleId);
        return list.Select(s => new SensorResponse(s.Id, s.Type, s.Unit, s.MinSafe, s.MaxSafe)).ToList();
    }

    public async Task<List<ReadingResponse>> GetLatestReadingsAsync(int moduleId)
    {
        _ = await modules.GetByIdAsync(moduleId) ?? throw new NotFoundException("Módulo não encontrado.");

        var moduleSensors = await sensors.GetByModuleAsync(moduleId);
        var latest = await readings.GetLatestPerSensorForModuleAsync(moduleId);
        // Pode haver empate quando duas leituras caem no mesmo segundo (recorded_at tem
        // precisão de segundos); nesse caso fica a de maior Id, a mais recente de fato.
        var latestBySensor = latest
            .GroupBy(r => r.SensorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Id).First());

        return moduleSensors.Select(s =>
        {
            if (latestBySensor.TryGetValue(s.Id, out var r))
            {
                var status = AlertRules.StatusLabel(s.Type, r.Value, s.MinSafe, s.MaxSafe);
                var comfort = AlertRules.ComfortLabel(s.Type, r.Value, s.MinSafe, s.MaxSafe);
                return new ReadingResponse(s.Id, s.Type, s.Unit, r.Value, r.RecordedAt, s.MinSafe, s.MaxSafe, status, comfort);
            }
            // Sensor sem leitura (ex.: módulo em manutenção).
            return new ReadingResponse(s.Id, s.Type, s.Unit, null, null, s.MinSafe, s.MaxSafe, "no-data", "Sem dados");
        }).ToList();
    }
}
