using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;
using P11.Api.Services;
using P11.Api.Services.Email;

namespace P11.Tests.Patterns;

/// <summary>Adapter: CorreoSmtpService traduce ICorreoService a la API de correo de .NET.</summary>
public class AdapterTests
{
    private static CorreoSmtpService Adaptador(int puerto = 1025) => new(Options.Create(new CorreoOptions
    {
        Host = "localhost",
        Puerto = puerto,
        Remitente = "no-responder@p11.local",
        NombreRemitente = "Plataforma de Inversión Ganadera",
    }), NullLogger<CorreoSmtpService>.Instance);

    [Fact]
    public void Traduce_la_llamada_del_sistema_a_un_MailMessage()
    {
        using var msj = Adaptador().ArmarMensaje("maria@test.bo", "Tu código de verificación: 123456", "Vence en 10 minutos.");

        Assert.Equal("no-responder@p11.local", msj.From!.Address);
        Assert.Equal("Plataforma de Inversión Ganadera", msj.From.DisplayName);
        Assert.Equal("maria@test.bo", Assert.Single(msj.To).Address);
        Assert.Equal("Tu código de verificación: 123456", msj.Subject);
        Assert.Equal("Vence en 10 minutos.", msj.Body);
        Assert.False(msj.IsBodyHtml);
    }

    [Fact]
    public async Task Si_el_servidor_no_responde_el_error_llega_a_quien_llama()
    {
        // Nadie escucha en el puerto 1: quien llama decide qué hacer (reintentar, avisar al usuario).
        ICorreoService correo = Adaptador(puerto: 1);
        await Assert.ThrowsAsync<SmtpException>(() => correo.EnviarAsync("maria@test.bo", "Hola", "Prueba"));
    }
}
