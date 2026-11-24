using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;

namespace P11.Api.Services.Email;

public interface ICorreoService
{
    Task EnviarAsync(string para, string asunto, string cuerpoTexto, CancellationToken ct = default);
}

/// <summary>
/// Adapter: el resto del sistema solo conoce ICorreoService.EnviarAsync(para, asunto, cuerpo);
/// esta clase lo traduce a la API de .NET (SmtpClient, MailMessage, MailAddress, credenciales).
/// En desarrollo apunta a Mailpit (http://localhost:8025).
/// </summary>
public class CorreoSmtpService(IOptions<CorreoOptions> opciones, ILogger<CorreoSmtpService> log) : ICorreoService
{
    private readonly CorreoOptions _o = opciones.Value;

    public async Task EnviarAsync(string para, string asunto, string cuerpoTexto, CancellationToken ct = default)
    {
        using var cliente = new SmtpClient(_o.Host, _o.Puerto) { EnableSsl = _o.UsarSsl };
        if (!string.IsNullOrEmpty(_o.Usuario))
            cliente.Credentials = new NetworkCredential(_o.Usuario, _o.Contrasena);

        using var msj = ArmarMensaje(para, asunto, cuerpoTexto);
        await cliente.SendMailAsync(msj, ct);
        log.LogInformation("Correo «{Asunto}» enviado a {Para}", asunto, para);
    }

    public MailMessage ArmarMensaje(string para, string asunto, string cuerpoTexto) =>
        new(new MailAddress(_o.Remitente, _o.NombreRemitente), new MailAddress(para))
        {
            Subject = asunto,
            Body = cuerpoTexto,
            IsBodyHtml = false,
        };
}
