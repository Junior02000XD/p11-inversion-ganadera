namespace P11.Api.Services.Email;

/// <summary>
/// Decorator: agrega reintentos a cualquier ICorreoService. Un corte breve del servidor SMTP
/// ya no deja al usuario sin su código OTP (RF-01) ni sin el aviso de bloqueo (RF-02).
/// </summary>
public class CorreoConReintentos(ICorreoService interno, ILogger<CorreoConReintentos> log, int intentos = 3, int esperaMs = 500)
    : ICorreoService
{
    public async Task EnviarAsync(string para, string asunto, string cuerpoTexto, CancellationToken ct = default)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                await interno.EnviarAsync(para, asunto, cuerpoTexto, ct);
                return;
            }
            catch (Exception ex) when (intento < intentos && !ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Falló el envío a {Para} (intento {Intento} de {Total}); se reintenta", para, intento, intentos);
                await Task.Delay(esperaMs * intento, ct);
            }
        }
    }
}
