using System.ComponentModel.DataAnnotations;
using P11.Api.Models;
using P11.Api.Models.Projects;
using P11.Api.Models.Simulation;
using P11.Api.Services.Investments;

namespace P11.Api.Dtos;

// Catálogo de proyectos publicados y simulador de escenarios (RF-11, RF-12).

// ---- Entrada ----

/// <summary>
/// Monto que el inversionista quiere simular y, si quiere probar otros, sus propios supuestos (en %).
/// Un supuesto que no se envía toma el valor que cargó el analista al publicar.
/// </summary>
public class SimulacionRequest
{
    [Range(1, 1_000_000_000, ErrorMessage = "Indica un monto mayor a 0.")]
    public decimal MontoBs { get; set; }

    [Range(-100, 300, ErrorMessage = "El rendimiento debe estar entre −100 % y 300 %.")]
    public decimal? RendimientoPct { get; set; }

    [Range(0, 100, ErrorMessage = "Los costos deben estar entre 0 % y 100 %.")]
    public decimal? CostosPct { get; set; }

    [Range(0, 100, ErrorMessage = "La mortalidad debe estar entre 0 % y 100 %.")]
    public decimal? MortalidadPct { get; set; }

    [Range(0, 100, ErrorMessage = "La variación debe estar entre 0 % y 100 %.")]
    public decimal? VariacionPct { get; set; }
}

// ---- Salida ----

/// <summary>Un proyecto publicado tal como lo ve el inversionista, con lo recaudado y el cupo libre.</summary>
public record ProyectoCatalogoDto(
    // Datos del proyecto
    Guid Id,
    LineaProductiva Linea,
    string Nombre,
    string Ubicacion,
    int PlazoMeses,

    // Financiamiento
    decimal MetaBs,
    decimal MinimoBs,
    decimal RecaudadoBs,
    decimal ReservadoBs,
    decimal CupoLibreBs,
    decimal PorcentajeFinanciado,

    // Riesgos y supuestos de los escenarios
    NivelRiesgo? NivelRiesgo,
    string? Riesgos,
    decimal? RendimientoPct,
    decimal? CostosPct,
    decimal? MortalidadPct,
    decimal? VariacionPct,

    DateTimeOffset? CierreRondaEn)
{
    public static ProyectoCatalogoDto Desde(ProyectoGanadero p, CupoProyecto cupo) => new(
        Id: p.Id,
        Linea: p.Linea,
        Nombre: p.Nombre,
        Ubicacion: p.Ubicacion,
        PlazoMeses: p.PlazoMeses,

        MetaBs: p.MontoRequeridoBs,
        MinimoBs: p.MontoMinimoBs,
        RecaudadoBs: cupo.RecaudadoBs,
        ReservadoBs: cupo.ReservadoBs,
        CupoLibreBs: cupo.LibreBs,
        PorcentajeFinanciado: cupo.PorcentajeFinanciado,

        NivelRiesgo: p.NivelRiesgo,
        Riesgos: p.Riesgos,
        RendimientoPct: p.RendimientoPct,
        CostosPct: p.CostosPct,
        MortalidadPct: p.MortalidadPct,
        VariacionPct: p.VariacionPct,

        CierreRondaEn: p.CierreRondaEn);
}

/// <summary>Un escenario del simulador (optimista, esperado o pesimista) con sus supuestos y su resultado.</summary>
public record EscenarioDto(
    TipoEscenario Tipo,
    decimal RendimientoPct,
    decimal CostosPct,
    decimal MortalidadPct,
    decimal ValorEstimadoBs,
    decimal ResultadoBs);

/// <summary>
/// RF-12: tres escenarios con sus supuestos y la advertencia de que no son garantizados (RNF-06).
/// SupuestosPropios indica que el inversionista cambió alguno de los supuestos del analista.
/// </summary>
public record SimulacionDto(
    Guid ProyectoId,
    decimal MontoBs,
    decimal MinimoBs,
    IReadOnlyList<EscenarioDto> Escenarios,
    bool SupuestosPropios,
    string Advertencia,
    string? Riesgos);
