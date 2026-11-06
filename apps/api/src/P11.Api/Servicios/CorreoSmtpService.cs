using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;

namespace P11.Api.Servicios;

public interface ICorreoService
{
    Task EnviarAsync(string para, string asunto, string cuerpoTexto, CancellationToken ct = default);
}

/// <summary>Envío por SMTP. En desarrollo apunta a Mailpit (http://localhost:8025).</summary>
public class CorreoSmtpService(IOptions<CorreoOptions> opciones, ILogger<CorreoSmtpService> log) : ICorreoService
{
    private readonly CorreoOptions _o = opciones.Value;

    public async Task EnviarAsync(string para, string asunto, string cuerpoTexto, CancellationToken ct = default)
    {
        using var cliente = new SmtpClient(_o.Host, _o.Puerto) { EnableSsl = _o.UsarSsl };
        if (!string.IsNullOrEmpty(_o.Usuario))
            cliente.Credentials = new NetworkCredential(_o.Usuario, _o.Contrasena);

        using var msj = new MailMessage(new MailAddress(_o.Remitente, _o.NombreRemitente), new MailAddress(para))
        {
            Subject = asunto,
            Body = cuerpoTexto,
            IsBodyHtml = false,
        };
        await cliente.SendMailAsync(msj, ct);
        log.LogInformation("Correo «{Asunto}» enviado a {Para}", asunto, para);
    }
}
