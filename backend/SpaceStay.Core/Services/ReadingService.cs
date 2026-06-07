using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class ReadingService(
    ISensorRepository sensors,
    ISensorReadingRepository readings,
    IAlertRepository alerts) : IReadingService
{
    public async Task<ReadingIngestResponse> IngestAsync(CreateReadingRequest request)
    {
        // Resolve o sensor (com o módulo, usado na mensagem); 404 se não existir.
        var sensor = await sensors.GetByIdWithModuleAsync(request.SensorId)
            ?? throw new NotFoundException("Sensor não encontrado.");

        var reading = new SensorReading
        {
            SensorId = sensor.Id,
            Value = request.Value,
            RecordedAt = request.RecordedAt ?? DateTime.UtcNow
        };
        await readings.AddAsync(reading);
        await readings.SaveChangesAsync();

        // Dentro da faixa segura: nenhum alerta.
        var severity = AlertRules.Evaluate(sensor.Type, request.Value, sensor.MinSafe, sensor.MaxSafe);
        if (severity is null)
            return new ReadingIngestResponse(reading.Id, OutOfRange: false, AlertCreated: false, Alert: null);

        // Fora da faixa: cria o alerta em aberto.
        var moduleName = sensor.Module?.Name ?? $"Módulo {sensor.ModuleId}";
        var message = AlertRules.BuildMessage(sensor.Type, request.Value, sensor.Unit, moduleName, severity.Value);

        var alert = new Alert
        {
            SensorId = sensor.Id,
            ModuleId = sensor.ModuleId,
            Severity = severity.Value,
            Message = message,
            Status = AlertStatus.open,
            CreatedAt = DateTime.UtcNow
        };
        await alerts.AddAsync(alert);
        await alerts.SaveChangesAsync();

        var dto = new AlertResponse(alert.Id, alert.SensorId, sensor.Type, alert.ModuleId, moduleName,
            alert.Severity, alert.Message, alert.Status, alert.CreatedAt, null, null);

        return new ReadingIngestResponse(reading.Id, OutOfRange: true, AlertCreated: true, Alert: dto);
    }
}
