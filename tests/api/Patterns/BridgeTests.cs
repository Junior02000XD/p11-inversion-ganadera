using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;
using P11.Api.Models;
using P11.Api.Services.Notifications;
using P11.Tests.Integration;

namespace P11.Tests.Patterns;

/// <summary>Bridge: el mismo aviso sale por cualquier canal sin cambiar su código.</summary>
public class BridgeTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 9, 14, 0, 0, TimeSpan.Zero);

    // Responde 200 a todo y guarda la última petición, como si fuera la API de Telegram.
    private class TelegramFalso : HttpMessageHandler
    {
        public Uri? Url { get; private set; }
        public JsonElement Cuerpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri;
            Cuerpo = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static CanalTelegram Telegram(TelegramFalso falso) =>
        new(new HttpClient(falso), Options.Create(new TelegramOptions { Token = "123:abc" }));

    [Fact]
    public async Task El_aviso_de_bloqueo_sale_por_correo()
    {
        var correo = new CorreoEnMemoria();

        await new AvisoBloqueo(new CanalCorreo(correo), 5, Ahora, Ahora.AddMinutes(15)).EnviarAsync("maria@test.bo");

        var enviado = Assert.Single(correo.Enviados);
        Assert.Equal("maria@test.bo", enviado.Para);
        Assert.Equal("Bloqueamos tu cuenta por seguridad", enviado.Asunto);
        Assert.Contains("hasta las 10:15", enviado.Cuerpo); // 14:15 UTC = 10:15 en Bolivia
    }

    [Fact]
    public async Task El_mismo_aviso_sale_por_Telegram_y_suena_porque_es_urgente()
    {
        var falso = new TelegramFalso();

        await new AvisoBloqueo(Telegram(falso), 5, Ahora, Ahora.AddMinutes(15)).EnviarAsync("987654");

        Assert.Equal("https://api.telegram.org/bot123:abc/sendMessage", falso.Url!.ToString());
        Assert.Equal("987654", falso.Cuerpo.GetProperty("chat_id").GetString());
        Assert.StartsWith("Bloqueamos tu cuenta por seguridad", falso.Cuerpo.GetProperty("text").GetString());
        Assert.False(falso.Cuerpo.GetProperty("disable_notification").GetBoolean());
    }

    [Fact]
    public async Task Un_aviso_que_no_es_urgente_llega_sin_sonido()
    {
        var falso = new TelegramFalso();

        await new AvisoVerificacion(Telegram(falso), EstadoVerificacion.Verificado, "Coincidencia 92 %").EnviarAsync("987654");

        Assert.StartsWith("Verificación de identidad: Verificado", falso.Cuerpo.GetProperty("text").GetString());
        Assert.True(falso.Cuerpo.GetProperty("disable_notification").GetBoolean());
    }
}
