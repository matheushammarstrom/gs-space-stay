using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;
using SpaceStay.Core.Services;
using SpaceStay.Tests.Unit.Fakes;
using Xunit;

namespace SpaceStay.Tests.Unit;

// Testes do ReadingService, a regra IoT central de limiar de alerta. Cobre TC5
// (leitura segura, sem alerta), TC6 (leitura crítica gera alerta) e TC8 (valor-limite).
public class ReadingServiceTests
{
    private readonly FakeSensorRepository _sensors = new();
    private readonly FakeSensorReadingRepository _readings = new();
    private readonly FakeAlertRepository _alerts = new();

    public ReadingServiceTests()
    {
        // Sensor de CO₂ no Cupola Suite 3 (faixa segura 0..1000 ppm).
        _sensors.Items.Add(new Sensor
        {
            Id = 1, ModuleId = 1, Type = SensorType.co2, Unit = "ppm", MinSafe = 0m, MaxSafe = 1000m,
            Module = new Module { Id = 1, Name = "Cupola Suite 3" }
        });
    }

    private ReadingService CreateSut() => new(_sensors, _readings, _alerts);

    [Fact] // TC5: leitura dentro da faixa não gera alerta
    public async Task Ingest_leitura_segura_nao_cria_alerta()
    {
        var resp = await CreateSut().IngestAsync(new CreateReadingRequest { SensorId = 1, Value = 600m });

        Assert.False(resp.OutOfRange);
        Assert.False(resp.AlertCreated);
        Assert.Null(resp.Alert);
        Assert.Single(_readings.Items);   // a leitura foi persistida
        Assert.Empty(_alerts.Items);      // mas nenhum alerta
    }

    [Fact] // TC6: leitura crítica gera alerta crítico
    public async Task Ingest_leitura_critica_cria_alerta_critico()
    {
        var resp = await CreateSut().IngestAsync(new CreateReadingRequest { SensorId = 1, Value = 2150m });

        Assert.True(resp.OutOfRange);
        Assert.True(resp.AlertCreated);
        Assert.NotNull(resp.Alert);
        Assert.Equal(AlertSeverity.critical, resp.Alert!.Severity);
        Assert.Equal(AlertStatus.open, resp.Alert.Status);
        var alert = Assert.Single(_alerts.Items);
        Assert.Equal(1, alert.ModuleId);
    }

    [Fact] // TC8: valor-limite, exatamente no máximo é seguro
    public async Task Ingest_no_limite_maximo_e_seguro()
    {
        var resp = await CreateSut().IngestAsync(new CreateReadingRequest { SensorId = 1, Value = 1000m });

        Assert.False(resp.OutOfRange);
        Assert.False(resp.AlertCreated);
        Assert.Empty(_alerts.Items);
    }

    [Fact]
    public async Task Ingest_acima_de_1000_mas_ate_2000_gera_aviso()
    {
        var resp = await CreateSut().IngestAsync(new CreateReadingRequest { SensorId = 1, Value = 1500m });

        Assert.True(resp.AlertCreated);
        Assert.Equal(AlertSeverity.warning, resp.Alert!.Severity);
    }

    [Fact]
    public async Task Ingest_sensor_inexistente_lanca_notfound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateSut().IngestAsync(new CreateReadingRequest { SensorId = 999, Value = 100m }));
    }
}
