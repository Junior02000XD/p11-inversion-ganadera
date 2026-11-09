using Microsoft.Extensions.Options;
using P11.Api.Configuracion;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios.Avisos;

namespace P11.Api.Servicios;

public record ImagenVerificacion(TipoDocumento Tipo, string NombreArchivo, string ContentType, byte[] Datos);

/// <summary>
/// Facade de la verificación de identidad (RF-03, RF-04). Los controladores llaman a dos operaciones
/// y no tratan con el almacén cifrado, el proveedor, el umbral, el historial ni los avisos.
/// </summary>
public class VerificacionService(
    AppDbContext db,
    IAlmacenArchivos almacen,
    IVerificadorIdentidad verificador,
    ICanalAviso canal,
    IOptions<VerificacionOptions> opciones,
    TimeProvider reloj,
    ILogger<VerificacionService> log)
{
    public const string ResponsableSistema = "sistema";

    /// <summary>Guarda las imágenes cifradas, compara el rostro y deja al usuario Verificado o En revisión.</summary>
    public async Task EnviarAsync(Usuario usuario, IReadOnlyList<ImagenVerificacion> imagenes, CancellationToken ct = default)
    {
        usuario.ConsentimientoDatosEn = reloj.GetUtcNow();
        foreach (var imagen in imagenes)
        {
            db.Documentos.Add(new DocumentoArchivo
            {
                UsuarioId = usuario.Id,
                Tipo = imagen.Tipo,
                NombreOriginal = Path.GetFileName(imagen.NombreArchivo),
                ContentType = imagen.ContentType,
                ArchivoCifrado = await almacen.GuardarAsync(imagen.Datos, ct),
            });
        }

        var ciFrente = imagenes.Single(i => i.Tipo == TipoDocumento.CiFrente);
        var selfie = imagenes.Single(i => i.Tipo == TipoDocumento.Selfie);
        var comparacion = await verificador.CompararRostroAsync(ciFrente.Datos, selfie.Datos, selfie.NombreArchivo, ct);

        var umbral = opciones.Value.UmbralCoincidencia;
        var aprobado = comparacion.Coincidencia >= umbral;
        var motivo = aprobado
            ? $"Coincidencia {comparacion.Coincidencia:P0} (umbral {umbral:P0}, proveedor {comparacion.Proveedor})."
            : $"Coincidencia {comparacion.Coincidencia:P0}, menor al umbral {umbral:P0}: pasa a revisión manual.";

        await CambiarEstadoAsync(usuario, aprobado ? EstadoVerificacion.Verificado : EstadoVerificacion.EnRevision,
            ResponsableSistema, motivo, ct);
    }

    /// <summary>Decisión del analista sobre una verificación en revisión.</summary>
    public Task DecidirAsync(Usuario usuario, bool aprobar, string responsable, string motivo, CancellationToken ct = default) =>
        CambiarEstadoAsync(usuario, aprobar ? EstadoVerificacion.Verificado : EstadoVerificacion.Rechazado, responsable, motivo, ct);

    public static string Texto(EstadoVerificacion e) => e switch
    {
        EstadoVerificacion.EnRevision => "En revisión",
        _ => e.ToString(),
    };

    // Todo cambio queda en el historial y se avisa al usuario (RF-04).
    private async Task CambiarEstadoAsync(Usuario u, EstadoVerificacion nuevo, string responsable, string motivo, CancellationToken ct)
    {
        db.HistorialVerificaciones.Add(new HistorialVerificacion
        {
            UsuarioId = u.Id,
            EstadoAnterior = u.EstadoVerificacion,
            EstadoNuevo = nuevo,
            Responsable = responsable,
            Motivo = motivo,
        });
        u.EstadoVerificacion = nuevo;
        await db.SaveChangesAsync(ct);

        try
        {
            await new AvisoVerificacion(canal, nuevo, motivo).EnviarAsync(u.Email!, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo notificar el estado de verificación a {Correo}", u.Email);
        }
    }
}
