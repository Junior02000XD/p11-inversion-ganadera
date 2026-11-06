using P11.Api.Datos;
using P11.Api.Dominio;

namespace P11.Api.Servicios;

/// <summary>RF-04: todo cambio de estado de verificación queda registrado y se notifica al usuario.</summary>
public class VerificacionService(AppDbContext db, ICorreoService correo, ILogger<VerificacionService> log)
{
    public const string ResponsableSistema = "sistema";

    /// <summary>Cambia el estado y agrega la fila de historial. El guardado lo hace quien llama.</summary>
    public void CambiarEstado(Usuario u, EstadoVerificacion nuevo, string responsable, string motivo)
    {
        db.HistorialVerificaciones.Add(new HistorialVerificacion
        {
            UsuarioId = u.Id,
            EstadoAnterior = u.EstadoVerificacion,
            EstadoNuevo = nuevo,
            Responsable = responsable,
            Motivo = motivo,
        });
        u.EstadoVerificacion = nuevo;
    }

    public static string Texto(EstadoVerificacion e) => e switch
    {
        EstadoVerificacion.EnRevision => "En revisión",
        _ => e.ToString(),
    };

    public async Task NotificarAsync(Usuario u, string motivo, CancellationToken ct = default)
    {
        var texto = u.EstadoVerificacion switch
        {
            EstadoVerificacion.Verificado => "Verificamos tu identidad. Ya puedes operar en la plataforma.",
            EstadoVerificacion.EnRevision => "Recibimos tus documentos. Un analista va a revisar tu identidad.",
            EstadoVerificacion.Rechazado => "No pudimos verificar tu identidad. Puedes volver a intentarlo con fotos nuevas.",
            _ => "El estado de tu verificación cambió.",
        };
        try
        {
            await correo.EnviarAsync(u.Email!, $"Verificación de identidad: {Texto(u.EstadoVerificacion)}", $"{texto}\nMotivo: {motivo}", ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo notificar el estado de verificación a {Correo}", u.Email);
        }
    }
}
