using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dtos;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Services.Documents;
using P11.Api.Services.Verification;
using P11.Api.Services.Auth;

namespace P11.Api.Controllers;

/// <summary>Revisión manual de identidad (RF-03/04) y de los documentos del productor y del predio (RF-05).</summary>
[ApiController]
[Route("api/analista")]
[Authorize(Roles = $"{Roles.Analista},{Roles.Administrador}")]
public class AnalistaController(AppDbContext db, IVisorDocumentos visor, VerificacionService verificacion, TimeProvider reloj) : ControllerBase
{
    /// <summary>Verificaciones de identidad en revisión manual, con sus imágenes (RF-03).</summary>
    [HttpGet("verificaciones")]
    [ProducesResponseType<IReadOnlyList<VerificacionPendienteDto>>(StatusCodes.Status200OK)]
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
            .Where(d => ids.Contains(d.UsuarioId) && !DocumentoArchivo.TiposPredio.Contains(d.Tipo))
            .OrderByDescending(d => d.SubidoEn)
            .ToListAsync(ct);

        return pendientes.Select(p => new VerificacionPendienteDto(
            p.Id, p.NombreCompleto, p.Email!, p.Ci, p.Rol!, p.ConsentimientoDatosEn,
            // El envío más reciente de cada tipo (CI anverso, reverso y selfie).
            documentos.Where(d => d.UsuarioId == p.Id).DistinctBy(d => d.Tipo).Select(DocumentoDto.Desde).ToList()))
            .ToList();
    }

    /// <summary>Aprueba o rechaza una verificación en revisión (RF-04); rechazar exige motivo.</summary>
    [HttpPost("verificaciones/{usuarioId:guid}/decision")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respuesta>> DecidirVerificacion(Guid usuarioId, DecisionRequest req, CancellationToken ct)
    {
        var usuario = await db.Users.FindAsync([usuarioId], ct);
        if (usuario is null) return NotFound(new Respuesta("No encontramos al usuario."));
        if (usuario.EstadoVerificacion != EstadoVerificacion.EnRevision)
            return Conflict(new Respuesta($"La verificación está en estado {VerificacionService.Texto(usuario.EstadoVerificacion)}, no en revisión."));
        if (!req.Aprobar && string.IsNullOrWhiteSpace(req.Motivo))
            return BadRequest(new Respuesta("Para rechazar hay que indicar el motivo."));

        var motivo = string.IsNullOrWhiteSpace(req.Motivo) ? "Aprobado en revisión manual." : req.Motivo.Trim();
        await verificacion.DecidirAsync(usuario, req.Aprobar, User.Correo(), motivo, ct);
        return Ok(new Respuesta($"Verificación de {usuario.NombreCompleto}: {VerificacionService.Texto(usuario.EstadoVerificacion)}."));
    }

    /// <summary>Documentos del predio por estado, por defecto los pendientes (RF-05).</summary>
    [HttpGet("documentos")]
    [ProducesResponseType<IReadOnlyList<DocumentoRevisionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentoRevisionDto>>> DocumentosDelPredio(
        [FromQuery] EstadoDocumento estado = EstadoDocumento.Pendiente, CancellationToken ct = default)
    {
        return await (
            from d in db.Documentos
            join u in db.Users on d.UsuarioId equals u.Id
            where d.Estado == estado && DocumentoArchivo.TiposPredio.Contains(d.Tipo)
            orderby d.SubidoEn
            select new DocumentoRevisionDto(d.Id, d.UsuarioId, u.Email!, d.Tipo, d.NombreOriginal, d.Estado, d.SubidoEn))
            .ToListAsync(ct);
    }

    /// <summary>Aprueba o rechaza un documento del predio (RF-05); rechazar exige motivo.</summary>
    [HttpPost("documentos/{id:guid}/decision")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respuesta>> DecidirDocumento(Guid id, DecisionRequest req, CancellationToken ct)
    {
        var documento = await db.Documentos.FindAsync([id], ct);
        if (documento is null || !DocumentoArchivo.TiposPredio.Contains(documento.Tipo)) return NotFound(new Respuesta("No encontramos el documento."));
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archivo(Guid id, CancellationToken ct)
    {
        var archivo = await visor.AbrirAsync(id, User, ct);
        return archivo is null ? NotFound() : File(archivo.Datos, archivo.ContentType, archivo.Nombre);
    }
}
