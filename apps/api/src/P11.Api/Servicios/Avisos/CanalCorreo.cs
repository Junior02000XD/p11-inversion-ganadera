namespace P11.Api.Servicios.Avisos;

public class CanalCorreo(ICorreoService correo) : ICanalAviso
{
    public Task EntregarAsync(string destino, MensajeAviso mensaje, CancellationToken ct = default) =>
        correo.EnviarAsync(destino, mensaje.Asunto, mensaje.Cuerpo, ct);
}
