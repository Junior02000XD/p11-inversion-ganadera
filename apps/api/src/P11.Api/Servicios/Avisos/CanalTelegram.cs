using Microsoft.Extensions.Options;
using P11.Api.Configuracion;

namespace P11.Api.Servicios.Avisos;

/// <summary>
/// Envía el aviso por un bot de Telegram (método sendMessage de la Bot API). El destino es el chat_id.
/// Los avisos que no son urgentes llegan sin sonido.
/// </summary>
public class CanalTelegram(HttpClient http, IOptions<TelegramOptions> opciones) : ICanalAviso
{
    public async Task EntregarAsync(string destino, MensajeAviso mensaje, CancellationToken ct = default)
    {
        var url = $"https://api.telegram.org/bot{opciones.Value.Token}/sendMessage";
        var respuesta = await http.PostAsJsonAsync(url, new
        {
            chat_id = destino,
            text = $"{mensaje.Asunto}\n\n{mensaje.Cuerpo}",
            disable_notification = !mensaje.Urgente,
        }, ct);
        respuesta.EnsureSuccessStatusCode();
    }
}
