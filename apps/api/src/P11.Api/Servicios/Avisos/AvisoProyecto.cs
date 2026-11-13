using P11.Api.Dominio;

namespace P11.Api.Servicios.Avisos;

/// <summary>RF-09: aviso al productor sobre el estado de su proyecto.</summary>
public class AvisoProyecto(ICanalAviso canal, string proyecto, EstadoProyecto estado, EstadoProyecto anterior, string motivo) : Aviso(canal)
{
    protected override string Asunto => $"Tu proyecto «{proyecto}»: {Texto}";

    private string Texto => (anterior, estado) switch
    {
        (_, EstadoProyecto.EnRevision) => "en evaluación técnica",
        (EstadoProyecto.EnRevision, EstadoProyecto.Borrador) => "tiene observaciones",
        (_, EstadoProyecto.Rechazado) => "fue rechazado",
        (_, EstadoProyecto.EnFinanciamiento) => "fue publicado",
        _ => "cambió de estado",
    };

    protected override string Cuerpo => (anterior, estado) switch
    {
        (_, EstadoProyecto.EnRevision) => "Recibimos tu proyecto. Un analista va a revisar la ficha y los documentos.",
        (EstadoProyecto.EnRevision, EstadoProyecto.Borrador) =>
            $"El analista dejó observaciones. Corrige la ficha y vuelve a enviarla.\nObservaciones: {motivo}",
        (_, EstadoProyecto.Rechazado) => $"El proyecto no se va a publicar.\nMotivo: {motivo}",
        (_, EstadoProyecto.EnFinanciamiento) => "El proyecto ya está en el catálogo y puede recibir inversiones.",
        _ => $"Estado actual: {estado}.",
    };
}

/// <summary>RF-09: aviso a los analistas de que hay un proyecto nuevo por evaluar.</summary>
public class AvisoProyectoPorEvaluar(ICanalAviso canal, string proyecto) : Aviso(canal)
{
    protected override string Asunto => $"Proyecto por evaluar: «{proyecto}»";
    protected override string Cuerpo => $"El productor envió «{proyecto}» a evaluación técnica. Está en la lista de Evaluación.";
}
