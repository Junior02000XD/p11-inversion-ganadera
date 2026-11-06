using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios;

namespace P11.Api.Controllers;

/// <summary>RF-03 verificación de identidad (simulada), RF-04 estado, RNF-07 consentimiento, RNF-02 cifrado.</summary>
[ApiController]
[Route("api/verificacion")]
[Authorize(Roles = $"{Roles.Inversionista},{Roles.Productor}")]
public class VerificacionController(
    AppDbContext db,
    UserManager<Usuario> usuarios,
    IAlmacenArchivos almacen,
    IVerificadorIdentidad verificador,
    VerificacionService verificacion,
    IOptions<ArchivosOptions> archivosOpt,
    IOptions<VerificacionOptions> verificacionOpt,
    TimeProvider reloj) : ControllerBase
{
    public class SolicitudVerificacion
    {
        public IFormFile? CiFrente { get; set; }
        public IFormFile? CiDorso { get; set; }
        public IFormFile? Selfie { get; set; }

        /// <summary>RNF-07: sin consentimiento explícito no se guarda nada.</summary>
        public bool Consentimiento { get; set; }
    }

    [HttpGet("estado")]
    public async Task<ActionResult<EstadoVerificacionDto>> Estado(CancellationToken ct)
    {
        var id = User.IdUsuario();
        var usuario = await db.Users.FindAsync([id], ct);
        if (usuario is null) return Unauthorized();

        var historial = await db.HistorialVerificaciones
            .Where(h => h.UsuarioId == id)
            .OrderBy(h => h.Fecha)
            .Select(h => new HistorialDto(h.EstadoAnterior, h.EstadoNuevo, h.Fecha, h.Responsable, h.Motivo))
            .ToListAsync(ct);
        return new EstadoVerificacionDto(usuario.EstadoVerificacion, historial);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<EstadoVerificacionDto>> Enviar([FromForm] SolicitudVerificacion solicitud, CancellationToken ct)
    {
        if (!solicitud.Consentimiento)
            return BadRequest(new Respuesta("Necesitamos tu consentimiento explícito para tratar tu CI y tu selfie."));

        var usuario = await usuarios.FindByIdAsync(User.IdUsuario().ToString());
        if (usuario is null) return Unauthorized();
        if (usuario.EstadoVerificacion is EstadoVerificacion.Verificado or EstadoVerificacion.EnRevision)
            return Conflict(new Respuesta($"Tu verificación ya está en estado {VerificacionService.Texto(usuario.EstadoVerificacion)}."));

        var archivos = new Dictionary<TipoDocumento, IFormFile?>
        {
            [TipoDocumento.CiFrente] = solicitud.CiFrente,
            [TipoDocumento.CiDorso] = solicitud.CiDorso,
            [TipoDocumento.Selfie] = solicitud.Selfie,
        };
        var imagenes = new Dictionary<TipoDocumento, (IFormFile Archivo, byte[] Datos)>();
        foreach (var (tipo, archivo) in archivos)
        {
            var datos = await LeerImagenAsync(archivo, ct);
            if (archivo is not null && datos is not null) imagenes[tipo] = (archivo, datos);
        }

        var ilegibles = archivos.Keys.Except(imagenes.Keys).ToList();
        if (ilegibles.Count > 0)
            return UnprocessableEntity(new ImagenesIlegibles("Algunas imágenes no se pueden leer. Sube fotos nuevas (JPG o PNG).", ilegibles));

        usuario.ConsentimientoDatosEn = reloj.GetUtcNow();
        foreach (var (tipo, (archivo, datos)) in imagenes)
        {
            db.Documentos.Add(new DocumentoArchivo
            {
                UsuarioId = usuario.Id,
                Tipo = tipo,
                NombreOriginal = Path.GetFileName(archivo.FileName),
                ContentType = archivo.ContentType,
                ArchivoCifrado = await almacen.GuardarAsync(datos, ct),
            });
        }

        var selfie = imagenes[TipoDocumento.Selfie];
        var comparacion = await verificador.CompararRostroAsync(imagenes[TipoDocumento.CiFrente].Datos, selfie.Datos, selfie.Archivo.FileName, ct);
        var umbral = verificacionOpt.Value.UmbralCoincidencia;
        var aprobado = comparacion.Coincidencia >= umbral;
        var motivo = aprobado
            ? $"Coincidencia {comparacion.Coincidencia:P0} (umbral {umbral:P0}, proveedor {comparacion.Proveedor})."
            : $"Coincidencia {comparacion.Coincidencia:P0}, menor al umbral {umbral:P0}: pasa a revisión manual.";

        verificacion.CambiarEstado(usuario, aprobado ? EstadoVerificacion.Verificado : EstadoVerificacion.EnRevision,
            VerificacionService.ResponsableSistema, motivo);
        await db.SaveChangesAsync(ct);
        await verificacion.NotificarAsync(usuario, motivo, ct);
        return await Estado(ct);
    }

    /// <summary>Devuelve los bytes si el archivo es una imagen legible; si no, null.</summary>
    private async Task<byte[]?> LeerImagenAsync(IFormFile? archivo, CancellationToken ct)
    {
        var o = archivosOpt.Value;
        if (archivo is null || archivo.Length == 0 || archivo.Length > o.TamanoMaximoBytes) return null;

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        var datos = ms.ToArray();
        return ValidadorArchivos.EsImagenLegible(datos, o.TamanoMinimoImagenBytes) ? datos : null;
    }
}
