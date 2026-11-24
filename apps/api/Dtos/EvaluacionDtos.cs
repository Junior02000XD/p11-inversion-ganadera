using System.ComponentModel.DataAnnotations;
using P11.Api.Models;
using P11.Api.Models.Projects;

namespace P11.Api.Dtos;

// Evaluación técnica de proyectos por el analista (RF-09, RF-10).

/// <summary>Lo que puede decidir el analista sobre un proyecto en revisión.</summary>
public enum DecisionProyecto { Aprobar, Observar, Rechazar }

// ---- Entrada ----

/// <summary>
/// Decisión del analista. Observar y rechazar exigen texto.
/// Aprobar exige los riesgos y los supuestos de los tres escenarios (RF-10): rendimiento, costos y
/// mortalidad del escenario esperado, en % del monto invertido para todo el plazo, y cuánto sube o
/// baja el rendimiento en el optimista y el pesimista (variación).
/// </summary>
public class EvaluacionRequest
{
    // Nullable + Required: si falta, da 400 en vez de tomar el primer valor del enum (Aprobar).
    [Required(ErrorMessage = "Indica la decisión: Aprobar, Observar o Rechazar.")]
    public DecisionProyecto? Decision { get; set; }

    public string? Observaciones { get; set; }

    // Solo al aprobar

    public NivelRiesgo? NivelRiesgo { get; set; }

    [StringLength(1000)]
    public string? Riesgos { get; set; }

    public decimal? RendimientoPct { get; set; }

    public decimal? CostosPct { get; set; }

    public decimal? MortalidadPct { get; set; }

    public decimal? VariacionPct { get; set; }

    /// <summary>Pasa los datos de publicación al formato que usa el dominio.</summary>
    public DatosPublicacion ADatosPublicacion() => new(
        NivelRiesgo: NivelRiesgo,
        Riesgos: Riesgos,
        RendimientoPct: RendimientoPct,
        CostosPct: CostosPct,
        MortalidadPct: MortalidadPct,
        VariacionPct: VariacionPct);
}

// ---- Salida ----

/// <summary>Un paso del ciclo de vida del proyecto: de qué estado a cuál, quién y por qué.</summary>
public record HistorialProyectoDto(
    EstadoProyecto EstadoAnterior,
    EstadoProyecto EstadoNuevo,
    string Accion,
    string Responsable,
    string Motivo,
    DateTimeOffset Fecha)
{
    public static HistorialProyectoDto Desde(HistorialProyecto h) => new(
        EstadoAnterior: h.EstadoAnterior,
        EstadoNuevo: h.EstadoNuevo,
        Accion: h.Accion,
        Responsable: h.Responsable,
        Motivo: h.Motivo,
        Fecha: h.Fecha);
}

/// <summary>Un proyecto tal como lo ve el analista: la ficha, quién lo postuló y su historial.</summary>
public record ProyectoEvaluacionDto(
    ProyectoDto Proyecto,
    string Productor,
    string CorreoProductor,
    IReadOnlyList<HistorialProyectoDto> Historial);
