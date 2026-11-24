namespace P11.Api.Services.Notifications;

public interface ICanalAviso
{
    /// <summary>Entrega el mensaje. El destino depende del canal: un correo, un chat de Telegram, un dispositivo.</summary>
    Task EntregarAsync(string destino, MensajeAviso mensaje, CancellationToken ct = default);
}
