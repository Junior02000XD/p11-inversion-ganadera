namespace P11.Api.Models.Projects;

/// <summary>Datos que llegan del formulario. Cada creador toma solo los campos de su línea.</summary>
public record DatosFicha(
    LineaProductiva Linea,
    string Nombre,
    string Ubicacion,
    decimal MontoRequeridoBs,
    decimal MontoMinimoBs,
    int PlazoMeses,
    // Engorde
    int? CantidadAnimales = null,
    decimal? PesoInicialKg = null,
    decimal? PesoMetaKg = null,
    decimal? CostosEstimadosBs = null,
    // Lechería
    int? VacasEnOrdeno = null,
    decimal? LitrosPorDia = null,
    decimal? PrecioLitroBs = null);
