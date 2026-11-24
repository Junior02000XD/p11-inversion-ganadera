using P11.Api.Models.Projects.States;

namespace P11.Api.Models.Projects;

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
    /// <summary>Lo que se guarda en la base. Solo cambia a través de las operaciones de abajo (State).</summary>
    public EstadoProyecto Estado { get; private set; } = EstadoProyecto.Borrador;
    /// <summary>Última observación o motivo de rechazo del analista (RF-09).</summary>
    public string? Observaciones { get; private set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    // ---- Publicación (RF-10). Se llenan cuando el analista aprueba; antes son null. ----
    public NivelRiesgo? NivelRiesgo { get; private set; }
    /// <summary>Riesgos que lee el inversionista antes de invertir y en el contrato (RF-14, RNF-06).</summary>
    public string? Riesgos { get; private set; }
    /// <summary>Supuestos del escenario esperado y cuánto se mueven en el pesimista y el optimista (RF-12).</summary>
    public decimal? RendimientoPct { get; private set; }
    public decimal? CostosPct { get; private set; }
    public decimal? MortalidadPct { get; private set; }
    public decimal? VariacionPct { get; private set; }
    public DateTimeOffset? PublicadoEn { get; private set; }
    /// <summary>Hasta cuándo recibe inversiones. El cierre de la ronda (RF-17) es del Sprint 3.</summary>
    public DateTimeOffset? CierreRondaEn { get; private set; }

    public abstract LineaProductiva Linea { get; }

    /// <summary>Objeto estado del patrón State; decide qué operación se permite.</summary>
    public EstadoFicha EstadoActual => EstadoFicha.De(Estado);

    public void EnviarARevision() => Estado = EstadoActual.EnviarARevision().Valor;

    public void Observar(string observaciones)
    {
        Estado = EstadoActual.Observar().Valor;
        Observaciones = observaciones;
    }

    public void Rechazar(string motivo)
    {
        Estado = EstadoActual.Rechazar().Valor;
        Observaciones = motivo;
    }

    /// <summary>
    /// RF-10: el estado decide si se puede publicar (solo desde revisión técnica) y después se exigen
    /// los riesgos y los supuestos de los tres escenarios. Si falta algo, no cambia nada.
    /// </summary>
    public void AprobarYPublicar(DatosPublicacion datos, DateTimeOffset ahora)
    {
        var siguiente = EstadoActual.AprobarYPublicar();
        if (datos.Incumplido() is { } motivo) throw new TransicionInvalidaException(motivo);

        Estado = siguiente.Valor;
        NivelRiesgo = datos.NivelRiesgo;
        Riesgos = datos.Riesgos!.Trim();
        RendimientoPct = datos.RendimientoPct;
        CostosPct = datos.CostosPct;
        MortalidadPct = datos.MortalidadPct;
        VariacionPct = datos.VariacionPct;
        PublicadoEn = ahora;
        CierreRondaEn = ahora.AddDays(DatosPublicacion.DiasRonda);
    }

    /// <summary>
    /// Corrige la ficha con datos nuevos (PUT). El estado decide si se puede (State) y después se
    /// valida igual que al crear. La línea productiva no cambia: engorde y lechería son tipos distintos.
    /// Devuelve los errores; si hay alguno, quien llama no debe guardar.
    /// </summary>
    public IReadOnlyList<ErrorCampo> Actualizar(DatosFicha datos)
    {
        EstadoActual.ValidarEdicion();
        if (datos.Linea != Linea)
            return [new(nameof(DatosFicha.Linea), "La línea productiva no se puede cambiar; crea un proyecto nuevo.")];

        Nombre = datos.Nombre?.Trim() ?? "";
        Ubicacion = datos.Ubicacion?.Trim() ?? "";
        MontoRequeridoBs = datos.MontoRequeridoBs;
        MontoMinimoBs = datos.MontoMinimoBs;
        PlazoMeses = datos.PlazoMeses;
        CopiarDatosLinea(datos);
        return Validar();
    }

    /// <summary>Copia los campos propios de la línea (los mismos que toma su creador).</summary>
    protected abstract void CopiarDatosLinea(DatosFicha datos);

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

    protected override void CopiarDatosLinea(DatosFicha d)
    {
        CantidadAnimales = d.CantidadAnimales ?? 0;
        PesoInicialKg = d.PesoInicialKg ?? 0;
        PesoMetaKg = d.PesoMetaKg ?? 0;
        CostosEstimadosBs = d.CostosEstimadosBs ?? 0;
    }

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

    protected override void CopiarDatosLinea(DatosFicha d)
    {
        VacasEnOrdeno = d.VacasEnOrdeno ?? 0;
        LitrosPorDia = d.LitrosPorDia ?? 0;
        PrecioLitroBs = d.PrecioLitroBs ?? 0;
    }

    protected override IEnumerable<ErrorCampo> ValidarLinea()
    {
        if (VacasEnOrdeno <= 0) yield return new(nameof(VacasEnOrdeno), "Indica las vacas en ordeño.");
        if (LitrosPorDia <= 0) yield return new(nameof(LitrosPorDia), "Indica los litros por día.");
        if (PrecioLitroBs <= 0) yield return new(nameof(PrecioLitroBs), "Indica el precio del litro de leche.");
    }
}
