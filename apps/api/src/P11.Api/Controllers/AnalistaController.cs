using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios;

namespace P11.Api.Controllers;

/// <summary>Revisión manual de identidad (RF-03/04) y de los documentos del productor y del predio (RF-05).</summary>
[ApiController]
[Route("api/analista")]
[Authorize(Roles = Roles.Analista)]
public class AnalistaController(AppDbContext db, IAlmacenArchivos almacen, VerificacionService verificacion, TimeProvider reloj) : ControllerBase
{
    private static readonly TipoDocumento[] TiposPredio = [TipoDocumento.Runsa, TipoDocumento.TituloPropiedad, TipoDocumento.ContratoAlquiler];

    [HttpGet("verificaciones")]
    public async Task<ActionResult<IReadOnlyList<VerificacionPendienteDto>>> VerificacionesEnRevision(CancellationToken ct)
    {
        var pendientes = await (
            from u in db.Users
            where u.EstadoVerificacion == EstadoVerificacion.EnRevision
            join ur in db.UserRoles on u.Id equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            select new { u.Id, u.NombreCompleto, u.Email, u.Ci, Rol = r.Name, u.ConsentimientoDatosEn })
            .ToListAsync(ct);

        var ids = pendientes.Select(p => p.Id).ToList();
        var documentos = await db.Documentos
            .Where(d => ids.Contains(d.UsuarioId) && !TiposPredio.Contains(d.Tipo))
            .OrderByDescending(d => d.SubidoEn)
            .ToListAsync(ct);

        return pendientes.Select(p => new VerificacionPendienteDto(
            p.Id, p.NombreCompleto, p.Email!, p.Ci, p.Rol!, p.ConsentimientoDatosEn,
            // El envío más reciente de cada tipo (CI anverso, reverso y selfie).
            documentos.Where(d => d.UsuarioId == p.Id).DistinctBy(d => d.Tipo).Select(DocumentoDto.Desde).ToList()))
            .ToList();
    }

    [HttpPost("verificaciones/{usuarioId:guid}/decision")]
    public async Task<ActionResult<Respuesta>> DecidirVerificacion(Guid usuarioId, DecisionRequest req, CancellationToken ct)
    {
        var usuario = await db.Users.FindAsync([usuarioId], ct);
        if (usuario is null) return NotFound(new Respuesta("No encontramos al usuario."));
        if (usuario.EstadoVerificacion != EstadoVerificacion.EnRevision)
            return Conflict(new Respuesta($"La verificación está en estado {VerificacionService.Texto(usuario.EstadoVerificacion)}, no en revisión."));
        if (!req.Aprobar && string.IsNullOrWhiteSpace(req.Motivo))
            return BadRequest(new Respuesta("Para rechazar hay que indicar el motivo."));

        var motivo = string.IsNullOrWhiteSpace(req.Motivo) ? "Aprobado en revisión manual." : req.Motivo.Trim();
        verificacion.CambiarEstado(usuario, req.Aprobar ? EstadoVerificacion.Verificado : EstadoVerificacion.Rechazado, User.Correo(), motivo);
        await db.SaveChangesAsync(ct);
        await verificacion.NotificarAsync(usuario, motivo, ct);
        return Ok(new Respuesta($"Verificación de {usuario.NombreCompleto}: {VerificacionService.Texto(usuario.EstadoVerificacion)}."));
    }

    [HttpGet("documentos")]
    public async Task<ActionResult<IReadOnlyList<DocumentoRevisionDto>>> DocumentosDelPredio(
        [FromQuery] EstadoDocumento estado = EstadoDocumento.Pendiente, CancellationToken ct = default)
    {
        return await (
            from d in db.Documentos
            join u in db.Users on d.UsuarioId equals u.Id
            where d.Estado == estado && TiposPredio.Contains(d.Tipo)
            orderby d.SubidoEn
            select new DocumentoRevisionDto(d.Id, d.UsuarioId, u.Email!, d.Tipo, d.NombreOriginal, d.Estado, d.SubidoEn))
            .ToListAsync(ct);
    }

    [HttpPost("documentos/{id:guid}/decision")]
    public async Task<ActionResult<Respuesta>> DecidirDocumento(Guid id, DecisionRequest req, CancellationToken ct)
    {
        var documento = await db.Documentos.FindAsync([id], ct);
        if (documento is null || !TiposPredio.Contains(documento.Tipo)) return NotFound(new Respuesta("No encontramos el documento."));
        if (!req.Aprobar && string.IsNullOrWhiteSpace(req.Motivo))
            return BadRequest(new Respuesta("Para rechazar hay que indicar el motivo."));

        documento.Estado = req.Aprobar ? EstadoDocumento.Aprobado : EstadoDocumento.Rechazado;
        documento.Motivo = req.Motivo?.Trim();
        documento.RevisadoPor = User.Correo();
        documento.RevisadoEn = reloj.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Ok(new Respuesta(req.Aprobar ? "Documento aprobado." : "Documento rechazado."));
    }

    /// <summary>Devuelve el archivo descifrado: solo la API, con su clave, puede abrirlo (RNF-02).</summary>
    [HttpGet("archivos/{id:guid}")]
    public async Task<IActionResult> Archivo(Guid id, CancellationToken ct)
    {
        var documento = await db.Documentos.FindAsync([id], ct);
        if (documento is null) return NotFound();

        var datos = await almacen.LeerAsync(documento.ArchivoCifrado, ct);
        var tipo = string.IsNullOrEmpty(documento.ContentType) ? "application/octet-stream" : documento.ContentType;
        return File(datos, tipo, documento.NombreOriginal);
    }
}
