using SpaceStay.Core.Domain;

namespace SpaceStay.Core.Services;

// Regra central de negócio, compartilhada pela ingestão de leituras (Parte 6) e pelos
// medidores de conforto do app (Parte 4): decide se uma leitura está fora da faixa
// segura e com qual gravidade, e monta a mensagem e o rótulo amigável.
public static class AlertRules
{
    // Devolve a gravidade quando a leitura está fora da faixa; null quando está segura.
    public static AlertSeverity? Evaluate(SensorType type, decimal value, decimal minSafe, decimal maxSafe)
    {
        var outOfRange = value < minSafe || value > maxSafe;
        if (!outOfRange) return null;

        return type switch
        {
            // CO₂: aviso acima de 1000 ppm, crítico acima de 2000 ppm.
            SensorType.co2 => value > 2000 ? AlertSeverity.critical : AlertSeverity.warning,
            // O₂ e pressão: qualquer desvio é crítico (risco à vida).
            SensorType.o2 or SensorType.pressure => AlertSeverity.critical,
            _ => AlertSeverity.warning
        };
    }

    public static string StatusLabel(SensorType type, decimal value, decimal minSafe, decimal maxSafe)
        => Evaluate(type, value, minSafe, maxSafe) switch
        {
            null => "safe",
            AlertSeverity.critical => "critical",
            _ => "warning"
        };

    public static string ComfortLabel(SensorType type, decimal value, decimal minSafe, decimal maxSafe)
        => Evaluate(type, value, minSafe, maxSafe) switch
        {
            null => "Ideal",
            AlertSeverity.critical => "Crítico",
            _ => "Atenção"
        };

    public static string BuildMessage(SensorType type, decimal value, string unit, string moduleName, AlertSeverity severity)
    {
        var grandeza = type switch
        {
            SensorType.o2 => "O₂",
            SensorType.co2 => "CO₂",
            SensorType.pressure => "Pressão",
            SensorType.temperature => "Temperatura",
            SensorType.humidity => "Umidade",
            SensorType.water => "Reserva de água",
            _ => type.ToString()
        };
        var nivel = severity == AlertSeverity.critical ? "nível crítico" : "fora da faixa de conforto";
        return $"{grandeza} em {value:0.##} {unit} em {moduleName}: {nivel}.";
    }
}
