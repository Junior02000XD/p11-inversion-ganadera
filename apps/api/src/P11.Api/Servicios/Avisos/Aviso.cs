namespace P11.Api.Servicios.Avisos;

public record MensajeAviso(string Asunto, string Cuerpo, bool Urgente);

/// <summary>
/// Bridge: el aviso decide qué se dice; el canal, por dónde llega.
/// Las dos jerarquías crecen por separado: un aviso nuevo sirve para todos los canales
/// y un canal nuevo (Telegram, push en Android) sirve para todos los avisos.
/// </summary>
public abstract class Aviso(ICanalAviso canal)
{
    protected abstract string Asunto { get; }
    protected abstract string Cuerpo { get; }
    protected virtual bool Urgente => false;

    public Task EnviarAsync(string destino, CancellationToken ct = default) =>
        canal.EntregarAsync(destino, new MensajeAviso(Asunto, Cuerpo, Urgente), ct);
}
