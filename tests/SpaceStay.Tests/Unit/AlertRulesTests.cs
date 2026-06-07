using SpaceStay.Core.Domain;
using SpaceStay.Core.Services;
using Xunit;

namespace SpaceStay.Tests.Unit;

// Testes da regra central de alerta. Usam análise de valor-limite e classes de
// equivalência (técnicas do curso) em torno dos limites de CO₂ (1000 e 2000 ppm) e
// das faixas de O₂, temperatura e água.
public class AlertRulesTests
{
    // safe | warning | critical, derivado de Evaluate (null = safe).
    private static string Label(AlertSeverity? s) => s switch
    {
        null => "safe",
        AlertSeverity.critical => "critical",
        _ => "warning"
    };

    [Theory]
    // CO₂ (min 0, max 1000): equivalência e valor-limite
    [InlineData(SensorType.co2, 600, 0, 1000, "safe")]      // dentro da faixa
    [InlineData(SensorType.co2, 1000, 0, 1000, "safe")]     // limite: exatamente no máximo = seguro
    [InlineData(SensorType.co2, 1001, 0, 1000, "warning")]  // limite: 1 acima = aviso
    [InlineData(SensorType.co2, 2000, 0, 1000, "warning")]  // limite: exatamente 2000 = aviso
    [InlineData(SensorType.co2, 2001, 0, 1000, "critical")] // limite: acima de 2000 = crítico
    [InlineData(SensorType.co2, 2150, 0, 1000, "critical")] // bem acima = crítico
    // O₂ (min 19.5, max 23.5): qualquer desvio é crítico (risco à vida)
    [InlineData(SensorType.o2, 20.9, 19.5, 23.5, "safe")]
    [InlineData(SensorType.o2, 19.5, 19.5, 23.5, "safe")]     // limite: no mínimo = seguro
    [InlineData(SensorType.o2, 19.4, 19.5, 23.5, "critical")] // abaixo = crítico
    [InlineData(SensorType.o2, 23.6, 19.5, 23.5, "critical")] // acima = crítico
    // pressão: desvio é crítico
    [InlineData(SensorType.pressure, 94.9, 95, 105, "critical")]
    // temperatura: desvio é aviso
    [InlineData(SensorType.temperature, 22, 18, 27, "safe")]
    [InlineData(SensorType.temperature, 28.5, 18, 27, "warning")]
    // água: reserva baixa é aviso
    [InlineData(SensorType.water, 18, 20, 100, "warning")]
    public void Evaluate_classifica_corretamente(SensorType type, double value, double min, double max, string esperado)
    {
        var severity = AlertRules.Evaluate(type, (decimal)value, (decimal)min, (decimal)max);
        Assert.Equal(esperado, Label(severity));
    }

    [Fact]
    public void StatusLabel_e_ComfortLabel_sao_consistentes()
    {
        // CO₂ crítico
        Assert.Equal("critical", AlertRules.StatusLabel(SensorType.co2, 2150m, 0m, 1000m));
        Assert.Equal("Crítico", AlertRules.ComfortLabel(SensorType.co2, 2150m, 0m, 1000m));
        // Dentro da faixa
        Assert.Equal("safe", AlertRules.StatusLabel(SensorType.co2, 600m, 0m, 1000m));
        Assert.Equal("Ideal", AlertRules.ComfortLabel(SensorType.co2, 600m, 0m, 1000m));
    }

    [Fact]
    public void BuildMessage_inclui_grandeza_valor_e_modulo()
    {
        var msg = AlertRules.BuildMessage(SensorType.co2, 2150m, "ppm", "Cupola Suite 3", AlertSeverity.critical);
        Assert.Contains("CO₂", msg);
        Assert.Contains("2150", msg);
        Assert.Contains("Cupola Suite 3", msg);
        Assert.Contains("crítico", msg);
    }
}
