namespace P11.Api.Models.Projects;

/// <summary>Resultado de crear un borrador: el proyecto o la lista de campos con error.</summary>
public record ResultadoCreacion(ProyectoGanadero? Proyecto, IReadOnlyList<ErrorCampo> Errores)
{
    public bool Exitoso => Proyecto is not null && Errores.Count == 0;
}

/// <summary>
/// Patrón Factory Method (GoF), creador abstracto. <see cref="CrearBorrador"/> es el flujo común de
/// postulación (RF-07); <see cref="FabricarProyecto"/> es el método fábrica que cada línea redefine.
/// La fábrica no aprueba nada: solo produce un borrador válido o informa los errores.
/// </summary>
public abstract class CreadorProyecto
{
    public abstract LineaProductiva Linea { get; }

    /// <summary>Método fábrica: crea el producto concreto con los campos propios de la línea.</summary>
    protected abstract ProyectoGanadero FabricarProyecto(DatosFicha datos);

    public ResultadoCreacion CrearBorrador(Guid productorId, DatosFicha datos)
    {
        if (datos.Linea != Linea)
            throw new ArgumentException($"Este creador produce proyectos de {Linea}, no de {datos.Linea}.");

        var proyecto = FabricarProyecto(datos);
        proyecto.ProductorId = productorId;
        proyecto.Nombre = datos.Nombre?.Trim() ?? "";
        proyecto.Ubicacion = datos.Ubicacion?.Trim() ?? "";
        proyecto.MontoRequeridoBs = datos.MontoRequeridoBs;
        proyecto.MontoMinimoBs = datos.MontoMinimoBs;
        proyecto.PlazoMeses = datos.PlazoMeses;

        var errores = proyecto.Validar();
        return errores.Count == 0 ? new(proyecto, errores) : new(null, errores);
    }
}

public sealed class CreadorProyectoEngorde : CreadorProyecto
{
    public override LineaProductiva Linea => LineaProductiva.Engorde;

    protected override ProyectoGanadero FabricarProyecto(DatosFicha d) => new ProyectoEngorde
    {
        CantidadAnimales = d.CantidadAnimales ?? 0,
        PesoInicialKg = d.PesoInicialKg ?? 0,
        PesoMetaKg = d.PesoMetaKg ?? 0,
        CostosEstimadosBs = d.CostosEstimadosBs ?? 0,
    };
}

public sealed class CreadorProyectoLecheria : CreadorProyecto
{
    public override LineaProductiva Linea => LineaProductiva.Lecheria;

    protected override ProyectoGanadero FabricarProyecto(DatosFicha d) => new ProyectoLecheria
    {
        VacasEnOrdeno = d.VacasEnOrdeno ?? 0,
        LitrosPorDia = d.LitrosPorDia ?? 0,
        PrecioLitroBs = d.PrecioLitroBs ?? 0,
    };
}

/// <summary>Elige el creador concreto según la línea; los creadores se registran en el contenedor de dependencias.</summary>
public class SelectorCreadorProyecto(IEnumerable<CreadorProyecto> creadores)
{
    private readonly Dictionary<LineaProductiva, CreadorProyecto> _porLinea = creadores.ToDictionary(c => c.Linea);

    public CreadorProyecto Para(LineaProductiva linea) =>
        _porLinea.TryGetValue(linea, out var c) ? c : throw new ArgumentOutOfRangeException(nameof(linea), linea, "Línea sin creador registrado.");
}
