using Microsoft.Extensions.Options;
using P11.Api.Configuration;

namespace P11.Api.Services.Verification;

public record ResultadoVerificacionExterna(double Coincidencia, string Proveedor);

/// <summary>
/// Proveedor externo de verificación de identidad (RF-03; en el diagrama de despliegue es un nodo externo).
/// Todavía no hay convenio: por eso hoy se usa una implementación simulada detrás de esta interfaz.
/// </summary>
public interface IVerificadorIdentidad
{
    Task<ResultadoVerificacionExterna> CompararRostroAsync(byte[] ciFrente, byte[] selfie, string nombreSelfie, CancellationToken ct = default);
}

/// <summary>
/// Simulación: devuelve el puntaje configurado. Para mostrar el camino de revisión manual en la demo,
/// una selfie cuyo nombre de archivo contiene «revision» recibe un puntaje bajo el umbral.
/// </summary>
public class VerificadorIdentidadSimulado(IOptions<VerificacionOptions> opciones) : IVerificadorIdentidad
{
    public Task<ResultadoVerificacionExterna> CompararRostroAsync(byte[] ciFrente, byte[] selfie, string nombreSelfie, CancellationToken ct = default)
    {
        var o = opciones.Value;
        var puntaje = nombreSelfie.Contains("revision", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(0, o.UmbralCoincidencia - 0.25)
            : o.PuntajeSimulado;
        return Task.FromResult(new ResultadoVerificacionExterna(puntaje, "simulado"));
    }
}
