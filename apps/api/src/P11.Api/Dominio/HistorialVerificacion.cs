namespace P11.Api.Dominio;

/// <summary>Cada cambio de estado de verificación guarda fecha, responsable y motivo (RF-04).</summary>
public class HistorialVerificacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public EstadoVerificacion EstadoAnterior { get; set; }
    public EstadoVerificacion EstadoNuevo { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>"sistema" o el correo del analista que decidió.</summary>
    public string Responsable { get; set; } = "";
    public string Motivo { get; set; } = "";
}
