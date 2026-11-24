using P11.Api.Models.Projects;

namespace P11.Api.Services.Projects;

/// <summary>
/// Command (GoF): cada acción sobre el ciclo de vida del proyecto es un objeto con quién la pide y por qué.
/// El invocador (<see cref="GestorCicloProyecto"/>) las ejecuta todas igual: aplica la transición,
/// guarda la bitácora (RNF-03) y avisa a los observadores. No hay «deshacer»: lo decidido queda como evidencia.
/// </summary>
public abstract record ComandoProyecto(Guid ProyectoId, string Responsable, string Motivo)
{
    /// <summary>Nombre de la acción en la bitácora.</summary>
    public abstract string Accion { get; }

    public abstract void Ejecutar(ProyectoGanadero proyecto);
}

/// <summary>Productor: envía el borrador a evaluación técnica (los requisitos los revisa la cadena antes).</summary>
public record EnviarARevision(Guid ProyectoId, string Responsable)
    : ComandoProyecto(ProyectoId, Responsable, "Enviado a evaluación técnica por el productor.")
{
    public override string Accion => "Enviar a revisión";
    public override void Ejecutar(ProyectoGanadero proyecto) => proyecto.EnviarARevision();
}

/// <summary>Analista (RF-09): vuelve al productor con observaciones.</summary>
public record ObservarProyecto(Guid ProyectoId, string Responsable, string Motivo) : ComandoProyecto(ProyectoId, Responsable, Motivo)
{
    public override string Accion => "Observar";
    public override void Ejecutar(ProyectoGanadero proyecto) => proyecto.Observar(Motivo);
}

/// <summary>Analista (RF-09): rechaza y guarda el motivo.</summary>
public record RechazarProyecto(Guid ProyectoId, string Responsable, string Motivo) : ComandoProyecto(ProyectoId, Responsable, Motivo)
{
    public override string Accion => "Rechazar";
    public override void Ejecutar(ProyectoGanadero proyecto) => proyecto.Rechazar(Motivo);
}

/// <summary>
/// Analista (RF-10): aprueba y publica con los riesgos y los supuestos de los tres escenarios.
/// Si falta alguno, el proyecto no cambia (lo valida el propio proyecto).
/// </summary>
public record AprobarYPublicarProyecto(Guid ProyectoId, string Responsable, string Motivo, DatosPublicacion Datos, DateTimeOffset Ahora)
    : ComandoProyecto(ProyectoId, Responsable, Motivo)
{
    public override string Accion => "Aprobar y publicar";
    public override void Ejecutar(ProyectoGanadero proyecto) => proyecto.AprobarYPublicar(Datos, Ahora);
}
