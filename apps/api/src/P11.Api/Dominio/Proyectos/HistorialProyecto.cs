namespace P11.Api.Dominio.Proyectos;

/// <summary>
/// Bitácora del ciclo de vida del proyecto (RF-09, RNF-03): cada cambio de estado guarda quién lo hizo,
/// cuándo, qué acción fue y por qué. Solo se agregan filas; no se modifican ni se borran.
/// </summary>
public class HistorialProyecto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProyectoId { get; set; }
    public EstadoProyecto EstadoAnterior { get; set; }
    public EstadoProyecto EstadoNuevo { get; set; }
    public string Accion { get; set; } = "";
    /// <summary>Correo de quien ejecutó la acción (productor o analista).</summary>
    public string Responsable { get; set; } = "";
    public string Motivo { get; set; } = "";
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
