namespace P11.Api.Dominio.Proyectos.Estados;

/// <summary>Se pidió una operación que el estado actual del proyecto no permite (la API responde 409).</summary>
public class TransicionInvalidaException(string mensaje) : InvalidOperationException(mensaje);

/// <summary>
/// Patrón State (GoF) para el ciclo de vida del proyecto (diagrama de estados EST-3).
/// Cada estado decide qué operaciones admite y a qué estado pasa; lo que no admite lanza
/// <see cref="TransicionInvalidaException"/>. La base de datos sigue guardando el enum
/// <see cref="EstadoProyecto"/>: el objeto estado se obtiene de ese valor con <see cref="De"/>.
/// Los estados no tienen datos propios, así que hay una sola instancia de cada uno.
/// </summary>
public abstract class EstadoFicha
{
    public abstract EstadoProyecto Valor { get; }

    /// <summary>Cómo se nombra el estado en una frase: «Un proyecto {Descripcion} no se puede…».</summary>
    protected abstract string Descripcion { get; }

    /// <summary>RF-07/RF-09: el productor solo corrige la ficha mientras está en borrador.</summary>
    public virtual bool PuedeEditarse => false;

    /// <summary>Solo un borrador se puede eliminar: lo que ya pasó por evaluación queda en la bitácora (RNF-03).</summary>
    public virtual bool PuedeEliminarse => false;

    /// <summary>Productor o administrador: corregir la ficha (PUT). Solo en borrador, también tras una observación.</summary>
    public void ValidarEdicion()
    {
        if (!PuedeEditarse) throw Invalida("editar");
    }

    /// <summary>Productor o administrador: eliminar la ficha (DELETE). Solo en borrador.</summary>
    public void ValidarEliminacion()
    {
        if (!PuedeEliminarse) throw Invalida("eliminar");
    }

    /// <summary>Productor: Borrador → En revisión técnica.</summary>
    public virtual EstadoFicha EnviarARevision() => throw Invalida("enviar a evaluación");

    /// <summary>Analista (RF-09): vuelve al productor con observaciones.</summary>
    public virtual EstadoFicha Observar() => throw Invalida("observar");

    /// <summary>Analista (RF-09): queda rechazado con su motivo y no se publica.</summary>
    public virtual EstadoFicha Rechazar() => throw Invalida("rechazar");

    /// <summary>Analista (RF-10): pasa al catálogo y empieza a recibir inversiones.</summary>
    public virtual EstadoFicha AprobarYPublicar() => throw Invalida("aprobar ni publicar");

    private TransicionInvalidaException Invalida(string accion) =>
        new($"Un proyecto {Descripcion} no se puede {accion}.");

    public static EstadoFicha De(EstadoProyecto valor) => valor switch
    {
        EstadoProyecto.Borrador => EnBorrador.Instancia,
        EstadoProyecto.EnRevision => EnRevisionTecnica.Instancia,
        EstadoProyecto.Rechazado => Rechazado.Instancia,
        EstadoProyecto.EnFinanciamiento => EnFinanciamiento.Instancia,
        _ => throw new ArgumentOutOfRangeException(nameof(valor), valor, "Estado sin clase asociada."),
    };
}

public sealed class EnBorrador : EstadoFicha
{
    public static readonly EnBorrador Instancia = new();
    public override EstadoProyecto Valor => EstadoProyecto.Borrador;
    protected override string Descripcion => "en borrador";
    public override bool PuedeEditarse => true;
    public override bool PuedeEliminarse => true;
    public override EstadoFicha EnviarARevision() => EnRevisionTecnica.Instancia;
}

public sealed class EnRevisionTecnica : EstadoFicha
{
    public static readonly EnRevisionTecnica Instancia = new();
    public override EstadoProyecto Valor => EstadoProyecto.EnRevision;
    protected override string Descripcion => "en revisión técnica";
    public override EstadoFicha Observar() => EnBorrador.Instancia;
    public override EstadoFicha Rechazar() => Rechazado.Instancia;
    public override EstadoFicha AprobarYPublicar() => EnFinanciamiento.Instancia;
}

/// <summary>Estado final: el motivo queda guardado y el proyecto no se publica (RF-09).</summary>
public sealed class Rechazado : EstadoFicha
{
    public static readonly Rechazado Instancia = new();
    public override EstadoProyecto Valor => EstadoProyecto.Rechazado;
    protected override string Descripcion => "rechazado";
}

/// <summary>
/// Publicado y recibiendo inversiones. El cierre de la ronda (RF-17: «Cerrado sin fondos» o
/// «En compra y traslado») llega con los Sprints 2 y 3; cada estado nuevo es una clase más.
/// </summary>
public sealed class EnFinanciamiento : EstadoFicha
{
    public static readonly EnFinanciamiento Instancia = new();
    public override EstadoProyecto Valor => EstadoProyecto.EnFinanciamiento;
    protected override string Descripcion => "en financiamiento";
}
