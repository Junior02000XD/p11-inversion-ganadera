using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;

namespace P11.Api.Controllers;

/// <summary>
/// CU «Consultar bitácora de auditoría» (RNF-03): une el historial de verificación de identidad y el de
/// los proyectos en una sola lista, de lo más nuevo a lo más viejo. Solo lectura.
/// </summary>
[ApiController]
[Route("api/admin/bitacora")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminBitacoraController(AppDbContext db) : ControllerBase
{
    private const int Maximo = 500;

    /// <summary>Consulta hasta 500 eventos de identidad y proyectos, del más reciente al más antiguo.</summary>
    /// <param name="desde">Fecha mínima inclusiva, opcional, en formato ISO 8601 con zona horaria.</param>
    /// <param name="hasta">Fecha máxima inclusiva, opcional, en formato ISO 8601 con zona horaria.</param>
    /// <param name="responsable">Parte del correo de quien hizo el cambio; filtro opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Hasta 500 eventos que coinciden con los filtros. La lista puede estar vacía.</response>
    /// <response code="400">Desde es posterior a hasta (Respuesta) o una fecha es inválida (ValidationProblemDetails).</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">Solo el administrador puede consultar la bitácora.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventoBitacoraDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventoBitacoraDto>>> Listar(
        DateTimeOffset? desde, DateTimeOffset? hasta, string? responsable, CancellationToken ct)
    {
        if (desde > hasta) return BadRequest(new Respuesta("La fecha «desde» no puede ser posterior a «hasta»."));

        var identidad = await (
            from h in db.HistorialVerificaciones
            join u in db.Users on h.UsuarioId equals u.Id into usuario
            from u in usuario.DefaultIfEmpty()
            where (desde == null || h.Fecha >= desde) && (hasta == null || h.Fecha <= hasta)
                && (responsable == null || h.Responsable.Contains(responsable))
            orderby h.Fecha descending
            select new { h, Nombre = u != null ? u.NombreCompleto : "(cuenta eliminada)" })
            .Take(Maximo).ToListAsync(ct);

        var proyectos = await (
            from h in db.HistorialProyectos
            join p in db.Proyectos on h.ProyectoId equals p.Id
            where (desde == null || h.Fecha >= desde) && (hasta == null || h.Fecha <= hasta)
                && (responsable == null || h.Responsable.Contains(responsable))
            orderby h.Fecha descending
            select new { h, p.Nombre })
            .Take(Maximo).ToListAsync(ct);

        return identidad
            .Select(x => new EventoBitacoraDto(x.h.Fecha, "Identidad", x.h.UsuarioId, x.Nombre, "Verificación de identidad",
                x.h.EstadoAnterior.ToString(), x.h.EstadoNuevo.ToString(), x.h.Responsable, x.h.Motivo))
            .Concat(proyectos.Select(x => new EventoBitacoraDto(x.h.Fecha, "Proyecto", x.h.ProyectoId, x.Nombre, x.h.Accion,
                x.h.EstadoAnterior.ToString(), x.h.EstadoNuevo.ToString(), x.h.Responsable, x.h.Motivo)))
            .OrderByDescending(e => e.Fecha)
            .Take(Maximo)
            .ToList();
    }
}
