using P11.Api.Services.Email;

namespace P11.Api.Services.Notifications;

public class CanalCorreo(ICorreoService correo) : ICanalAviso
{
    public Task EntregarAsync(string destino, MensajeAviso mensaje, CancellationToken ct = default) =>
        correo.EnviarAsync(destino, mensaje.Asunto, mensaje.Cuerpo, ct);
}
