using P11.Api.Models.Projects;
using P11.Api.Models.Simulation;

namespace P11.Api.Services.Simulation;

/// <summary>Un escenario calculado: los supuestos con que se armó y cuánto valdría el aporte al final del plazo.</summary>
public record EscenarioSimulado(TipoEscenario Tipo, decimal RendimientoPct, decimal CostosPct, decimal MortalidadPct, decimal ValorEstimadoBs)
{
    public decimal ResultadoBs(decimal montoBs) => ValorEstimadoBs - montoBs;
}

/// <summary>Supuestos que elige el inversionista, en %. Los que quedan en null toman el valor del analista.</summary>
public record SupuestosSimulacion(decimal? RendimientoPct, decimal? CostosPct, decimal? MortalidadPct, decimal? VariacionPct);

/// <summary>
/// RF-12: arma los tres escenarios con los supuestos que cargó el analista al publicar (RF-10).
/// Usa el Prototype: una configuración base con los supuestos del escenario esperado, que se clona
/// para el pesimista (rendimiento − variación) y el optimista (rendimiento + variación).
/// El inversionista puede reemplazar cualquiera de esos supuestos para probar otros casos.
/// </summary>
public static class SimuladorEscenarios
{
    public static IReadOnlyList<EscenarioSimulado> Simular(ProyectoGanadero proyecto, decimal montoBs, SupuestosSimulacion? propios = null)
    {
        var baseConfig = new ConfiguracionEscenario
        {
            ProyectoId = proyecto.Id,
            VersionSupuestos = 1,
            MontoInversionBs = montoBs,
            PlazoMeses = proyecto.PlazoMeses,
        };
        baseConfig.Supuestos["rendimiento"] = (propios?.RendimientoPct ?? proyecto.RendimientoPct ?? 0) / 100;
        baseConfig.Supuestos["costos"] = (propios?.CostosPct ?? proyecto.CostosPct ?? 0) / 100;
        baseConfig.Supuestos["mortalidad"] = (propios?.MortalidadPct ?? proyecto.MortalidadPct ?? 0) / 100;
        if (proyecto.Riesgos is not null) baseConfig.Riesgos.Add(proyecto.Riesgos);

        return PreparadorEscenarios.Preparar(baseConfig, (propios?.VariacionPct ?? proyecto.VariacionPct ?? 0) / 100)
            .Select(c => new EscenarioSimulado(
                c.Tipo,
                c.Supuestos["rendimiento"] * 100,
                c.Supuestos["costos"] * 100,
                c.Supuestos["mortalidad"] * 100,
                c.Calcular().ValorEstimadoBs))
            .ToList();
    }

    /// <summary>True si alguno de los supuestos enviados difiere del que cargó el analista.</summary>
    public static bool SonPropios(ProyectoGanadero proyecto, SupuestosSimulacion propios) =>
        Difiere(propios.RendimientoPct, proyecto.RendimientoPct) || Difiere(propios.CostosPct, proyecto.CostosPct)
        || Difiere(propios.MortalidadPct, proyecto.MortalidadPct) || Difiere(propios.VariacionPct, proyecto.VariacionPct);

    private static bool Difiere(decimal? propio, decimal? analista) => propio is not null && propio != (analista ?? 0);
}
