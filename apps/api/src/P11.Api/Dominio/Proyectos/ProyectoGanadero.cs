namespace P11.Api.Dominio.Proyectos;

/// <summary>Error de validación ligado a un campo de la ficha (RF-08: se marcan los campos con error).</summary>
public record ErrorCampo(string Campo, string Mensaje);

/// <summary>
/// Producto del Factory Method (RF-07). Cada línea productiva es una subclase con sus propios
/// campos e invariantes: por eso la creación se delega a un creador concreto por línea.
/// </summary>
public abstract class ProyectoGanadero
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductorId { get; set; }
    public string Nombre { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public decimal MontoRequeridoBs { get; set; }
    public decimal MontoMinimoBs { get; set; }
    public int PlazoMeses { get; set; }
    public EstadoProyecto Estado { get; set; } = EstadoProyecto.Borrador;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public abstract LineaProductiva Linea { get; }

    /// <summary>Valida la ficha: reglas comunes más las propias de la línea.</summary>
    public IReadOnlyList<ErrorCampo> Validar()
    {
        var errores = new List<ErrorCampo>();
        if (string.IsNullOrWhiteSpace(Nombre)) errores.Add(new(nameof(Nombre), "El nombre es obligatorio."));
        if (string.IsNullOrWhiteSpace(Ubicacion)) errores.Add(new(nameof(Ubicacion), "La ubicación es obligatoria."));
        if (MontoRequeridoBs <= 0) errores.Add(new(nameof(MontoRequeridoBs), "El monto requerido debe ser mayor a 0."));
        if (MontoMinimoBs <= 0) errores.Add(new(nameof(MontoMinimoBs), "El monto mínimo de inversión debe ser mayor a 0."));
        else if (MontoMinimoBs > MontoRequeridoBs) errores.Add(new(nameof(MontoMinimoBs), "El monto mínimo no puede superar al requerido."));
        if (PlazoMeses <= 0) errores.Add(new(nameof(PlazoMeses), "El plazo debe ser mayor a 0 meses."));
        errores.AddRange(ValidarLinea());
        return errores;
    }

    protected abstract IEnumerable<ErrorCampo> ValidarLinea();
}

/// <summary>Ficha de engorde: animales, peso inicial y meta, costos (RF-07).</summary>
public class ProyectoEngorde : ProyectoGanadero
{
    public override LineaProductiva Linea => LineaProductiva.Engorde;
    public int CantidadAnimales { get; set; }
    public decimal PesoInicialKg { get; set; }
    public decimal PesoMetaKg { get; set; }
    public decimal CostosEstimadosBs { get; set; }

    protected override IEnumerable<ErrorCampo> ValidarLinea()
    {
        if (CantidadAnimales <= 0) yield return new(nameof(CantidadAnimales), "Indica la cantidad de animales.");
        if (PesoInicialKg <= 0) yield return new(nameof(PesoInicialKg), "Indica el peso inicial promedio.");
        if (PesoMetaKg <= PesoInicialKg) yield return new(nameof(PesoMetaKg), "El peso meta debe ser mayor al inicial.");
        if (CostosEstimadosBs <= 0) yield return new(nameof(CostosEstimadosBs), "Indica los costos estimados.");
    }
}

/// <summary>Ficha de lechería: vacas en ordeño, litros por día, precio de la leche (RF-07).</summary>
public class ProyectoLecheria : ProyectoGanadero
{
    public override LineaProductiva Linea => LineaProductiva.Lecheria;
    public int VacasEnOrdeno { get; set; }
    public decimal LitrosPorDia { get; set; }
    public decimal PrecioLitroBs { get; set; }

    protected override IEnumerable<ErrorCampo> ValidarLinea()
    {
        if (VacasEnOrdeno <= 0) yield return new(nameof(VacasEnOrdeno), "Indica las vacas en ordeño.");
        if (LitrosPorDia <= 0) yield return new(nameof(LitrosPorDia), "Indica los litros por día.");
        if (PrecioLitroBs <= 0) yield return new(nameof(PrecioLitroBs), "Indica el precio del litro de leche.");
    }
}
