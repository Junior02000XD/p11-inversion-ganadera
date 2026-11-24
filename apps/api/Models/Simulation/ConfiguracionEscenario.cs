namespace P11.Api.Models.Simulation;

public enum TipoEscenario { Base, Pesimista, Esperado, Optimista }

/// <summary>Contrato de clonación del patrón Prototype.</summary>
public interface IPrototipo<out T> { T Clonar(); }

/// <summary>Resultado estimado de un escenario. Nunca es una rentabilidad garantizada (RNF-06).</summary>
public record ResultadoEscenario(TipoEscenario Tipo, decimal MontoInvertidoBs, decimal ValorEstimadoBs, string Advertencia);

/// <summary>
/// Patrón Prototype (GoF) para RF-12. La configuración base del simulador se copia para preparar
/// las variantes pesimista, esperada y optimista. Política de copia:
/// - colecciones modificables (supuestos, riesgos): se duplican, cada copia tiene las suyas;
/// - valores inmutables (proyecto, versión de supuestos, plazo, monto): se copian tal cual;
/// - identidad: cada copia recibe un IdSimulacion nuevo y su resultado anterior se descarta.
/// </summary>
public sealed class ConfiguracionEscenario : IPrototipo<ConfiguracionEscenario>
{
    public const string Advertencia = "Estimación, no rentabilidad garantizada. Revise los riesgos del proyecto.";

    public Guid IdSimulacion { get; private set; } = Guid.NewGuid();
    public Guid ProyectoId { get; init; }
    public int VersionSupuestos { get; init; }
    public TipoEscenario Tipo { get; private set; } = TipoEscenario.Base;
    public decimal MontoInversionBs { get; init; }
    public int PlazoMeses { get; init; }
    /// <summary>Supuestos numéricos (p. ej. "rendimiento", "costos", "mortalidad"), en fracción.</summary>
    public Dictionary<string, decimal> Supuestos { get; private set; } = new();
    public List<string> Riesgos { get; private set; } = [];
    public ResultadoEscenario? Resultado { get; private set; }

    public ConfiguracionEscenario Clonar() => new()
    {
        IdSimulacion = Guid.NewGuid(),
        ProyectoId = ProyectoId,
        VersionSupuestos = VersionSupuestos,
        Tipo = Tipo,
        MontoInversionBs = MontoInversionBs,
        PlazoMeses = PlazoMeses,
        Supuestos = new Dictionary<string, decimal>(Supuestos),
        Riesgos = [.. Riesgos],
        Resultado = null,
    };

    public ConfiguracionEscenario ComoVariante(TipoEscenario tipo, IReadOnlyDictionary<string, decimal> ajustes)
    {
        var copia = Clonar();
        copia.Tipo = tipo;
        foreach (var (clave, ajuste) in ajustes)
            copia.Supuestos[clave] = copia.Supuestos.GetValueOrDefault(clave) + ajuste;
        return copia;
    }

    /// <summary>
    /// Fórmula provisional (la definitiva está pendiente con el cliente, ERS RF-12):
    /// valor = monto × (1 + rendimiento − costos − mortalidad).
    /// </summary>
    public ResultadoEscenario Calcular()
    {
        var factor = 1 + Supuestos.GetValueOrDefault("rendimiento") - Supuestos.GetValueOrDefault("costos") - Supuestos.GetValueOrDefault("mortalidad");
        Resultado = new ResultadoEscenario(Tipo, MontoInversionBs, Math.Round(MontoInversionBs * factor, 2), Advertencia);
        return Resultado;
    }
}

/// <summary>Prepara las tres variantes a partir de una base, sin tocar la base.</summary>
public static class PreparadorEscenarios
{
    public static IReadOnlyList<ConfiguracionEscenario> Preparar(ConfiguracionEscenario baseConfig, decimal desvioRendimiento = 0.05m) =>
    [
        baseConfig.ComoVariante(TipoEscenario.Pesimista, new Dictionary<string, decimal> { ["rendimiento"] = -desvioRendimiento }),
        baseConfig.ComoVariante(TipoEscenario.Esperado, new Dictionary<string, decimal>()),
        baseConfig.ComoVariante(TipoEscenario.Optimista, new Dictionary<string, decimal> { ["rendimiento"] = desvioRendimiento }),
    ];
}
