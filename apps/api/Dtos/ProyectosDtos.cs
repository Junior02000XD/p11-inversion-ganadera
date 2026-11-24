using System.ComponentModel.DataAnnotations;
using P11.Api.Models;
using P11.Api.Models.Projects;

namespace P11.Api.Dtos;

// Proyectos: la ficha del proyecto ganadero (RF-07, RF-08).

// ---- Entrada ----

/// <summary>
/// Datos de la ficha que llegan en POST y PUT /api/proyectos.
/// Los atributos validan lo obligatorio y los rangos (si algo falla, la API responde 400 sola);
/// las reglas que dependen de la línea (engorde o lechería) las valida el dominio.
/// El administrador postula en nombre de un productor y debe indicar <see cref="ProductorId"/>;
/// el productor no lo envía.
/// </summary>
public class ProyectoEntradaDto
{
    // Datos comunes a las dos líneas

    [Required(ErrorMessage = "Indica la línea productiva (Engorde o Lecheria).")]
    public LineaProductiva? Linea { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(200)]
    public string Ubicacion { get; set; } = "";

    [Range(0.01, 1_000_000_000, ErrorMessage = "El monto requerido debe ser mayor a 0.")]
    public decimal MontoRequeridoBs { get; set; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "El monto mínimo debe ser mayor a 0.")]
    public decimal MontoMinimoBs { get; set; }

    [Range(1, 120, ErrorMessage = "El plazo debe estar entre 1 y 120 meses.")]
    public int PlazoMeses { get; set; }

    // Solo engorde

    [Range(1, 100_000, ErrorMessage = "Indica la cantidad de animales (entre 1 y 100.000).")]
    public int? CantidadAnimales { get; set; }

    [Range(0.01, 2_000, ErrorMessage = "Indica el peso inicial promedio (hasta 2.000 kg).")]
    public decimal? PesoInicialKg { get; set; }

    [Range(0.01, 2_000, ErrorMessage = "Indica el peso meta (hasta 2.000 kg).")]
    public decimal? PesoMetaKg { get; set; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "Indica los costos estimados.")]
    public decimal? CostosEstimadosBs { get; set; }

    // Solo lechería

    [Range(1, 100_000, ErrorMessage = "Indica las vacas en ordeño (entre 1 y 100.000).")]
    public int? VacasEnOrdeno { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Indica los litros por día.")]
    public decimal? LitrosPorDia { get; set; }

    [Range(0.01, 1_000, ErrorMessage = "Indica el precio del litro (hasta Bs 1.000).")]
    public decimal? PrecioLitroBs { get; set; }

    // Solo el administrador

    public Guid? ProductorId { get; set; }

    /// <summary>Pasa la ficha al formato que usa el dominio para crear o actualizar el proyecto.</summary>
    public DatosFicha ADatosFicha() => new(
        Linea: Linea!.Value,
        Nombre: Nombre,
        Ubicacion: Ubicacion,
        MontoRequeridoBs: MontoRequeridoBs,
        MontoMinimoBs: MontoMinimoBs,
        PlazoMeses: PlazoMeses,
        CantidadAnimales: CantidadAnimales,
        PesoInicialKg: PesoInicialKg,
        PesoMetaKg: PesoMetaKg,
        CostosEstimadosBs: CostosEstimadosBs,
        VacasEnOrdeno: VacasEnOrdeno,
        LitrosPorDia: LitrosPorDia,
        PrecioLitroBs: PrecioLitroBs);
}

// ---- Salida ----

/// <summary>Un proyecto completo: datos comunes, los de su línea y, si ya fue evaluado, los de publicación.</summary>
public record ProyectoDto(
    // Datos comunes
    Guid Id,
    LineaProductiva Linea,
    string Nombre,
    string Ubicacion,
    decimal MontoRequeridoBs,
    decimal MontoMinimoBs,
    int PlazoMeses,
    EstadoProyecto Estado,
    DateTimeOffset CreadoEn,

    // Solo engorde
    int? CantidadAnimales,
    decimal? PesoInicialKg,
    decimal? PesoMetaKg,
    decimal? CostosEstimadosBs,

    // Solo lechería
    int? VacasEnOrdeno,
    decimal? LitrosPorDia,
    decimal? PrecioLitroBs,

    // Evaluación y publicación
    string? Observaciones,
    NivelRiesgo? NivelRiesgo,
    string? Riesgos,
    decimal? RendimientoPct,
    decimal? CostosPct,
    decimal? MortalidadPct,
    decimal? VariacionPct,
    DateTimeOffset? PublicadoEn,
    DateTimeOffset? CierreRondaEn)
{
    public static ProyectoDto Desde(ProyectoGanadero p)
    {
        var engorde = p as ProyectoEngorde;
        var lecheria = p as ProyectoLecheria;

        return new(
            Id: p.Id,
            Linea: p.Linea,
            Nombre: p.Nombre,
            Ubicacion: p.Ubicacion,
            MontoRequeridoBs: p.MontoRequeridoBs,
            MontoMinimoBs: p.MontoMinimoBs,
            PlazoMeses: p.PlazoMeses,
            Estado: p.Estado,
            CreadoEn: p.CreadoEn,

            CantidadAnimales: engorde?.CantidadAnimales,
            PesoInicialKg: engorde?.PesoInicialKg,
            PesoMetaKg: engorde?.PesoMetaKg,
            CostosEstimadosBs: engorde?.CostosEstimadosBs,

            VacasEnOrdeno: lecheria?.VacasEnOrdeno,
            LitrosPorDia: lecheria?.LitrosPorDia,
            PrecioLitroBs: lecheria?.PrecioLitroBs,

            Observaciones: p.Observaciones,
            NivelRiesgo: p.NivelRiesgo,
            Riesgos: p.Riesgos,
            RendimientoPct: p.RendimientoPct,
            CostosPct: p.CostosPct,
            MortalidadPct: p.MortalidadPct,
            VariacionPct: p.VariacionPct,
            PublicadoEn: p.PublicadoEn,
            CierreRondaEn: p.CierreRondaEn);
    }
}
