using P11.Api.Models.Projects;
using P11.Api.Models.Simulation;

namespace P11.Api.Services.Simulation;

/// <summary>Un escenario calculado: los supuestos con que se armó y cuánto valdría el aporte al final del plazo.</summary>
public record EscenarioSimulado(TipoEscenario Tipo, decimal RendimientoPct, decimal CostosPct, decimal MortalidadPct, decimal ValorEstimadoBs)
{
    public decimal ResultadoBs(decimal montoBs) => ValorEstimadoBs - montoBs;
}

/// <summary>
/// RF-12: arma los tres escenarios con los supuestos que cargó el analista al publicar (RF-10).
/// Usa el Prototype: una configuración base con los supuestos del escenario esperado, que se clona
/// para el pesimista (rendimiento − variación) y el optimista (rendimiento + variación).
/// </summary>
public static class SimuladorEscenarios
{
    public static IReadOnlyList<EscenarioSimulado> Simular(ProyectoGanadero proyecto, decimal montoBs)
    {
        var baseConfig = new ConfiguracionEscenario
        {
            ProyectoId = proyecto.Id,
            VersionSupuestos = 1,
            MontoInversionBs = montoBs,
            PlazoMeses = proyecto.PlazoMeses,
        };
        baseConfig.Supuestos["rendimiento"] = (proyecto.RendimientoPct ?? 0) / 100;
        baseConfig.Supuestos["costos"] = (proyecto.CostosPct ?? 0) / 100;
        baseConfig.Supuestos["mortalidad"] = (proyecto.MortalidadPct ?? 0) / 100;
        if (proyecto.Riesgos is not null) baseConfig.Riesgos.Add(proyecto.Riesgos);

        return PreparadorEscenarios.Preparar(baseConfig, (proyecto.VariacionPct ?? 0) / 100)
            .Select(c => new EscenarioSimulado(
                c.Tipo,
                c.Supuestos["rendimiento"] * 100,
                c.Supuestos["costos"] * 100,
                c.Supuestos["mortalidad"] * 100,
                c.Calcular().ValorEstimadoBs))
            .ToList();
    }
}
