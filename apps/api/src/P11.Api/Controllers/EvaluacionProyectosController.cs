using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos.Estados;
using P11.Api.Servicios;
using P11.Api.Servicios.Proyectos;

namespace P11.Api.Controllers;

/// <summary>RF-09 evaluación técnica y RF-10 publicación, a cargo del analista.</summary>
[ApiController]
[Route("api/analista/proyectos")]
[Authorize(Roles = $"{Roles.Analista},{Roles.Administrador}")]
public class EvaluacionProyectosController(AppDbContext db, GestorCicloProyecto ciclo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProyectoEvaluacionDto>>> Listar(
        [FromQuery] EstadoProyecto estado = EstadoProyecto.EnRevision, CancellationToken ct = default)
    {
        var filas = await (
            from p in db.Proyectos
            join u in db.Users on p.ProductorId equals u.Id
            where p.Estado == estado
            orderby p.CreadoEn
            select new { Proyecto = p, u.NombreCompleto, u.Email })
            .ToListAsync(ct);
        return filas.Select(f => new ProyectoEvaluacionDto(ProyectoDto.Desde(f.Proyecto), f.NombreCompleto, f.Email!, [])).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProyectoEvaluacionDto>> Obtener(Guid id, CancellationToken ct)
    {
        var proyecto = await db.Proyectos.FindAsync([id], ct);
        if (proyecto is null) return NotFound(new Respuesta("No encontramos el proyecto."));
        var productor = await db.Users.FindAsync([proyecto.ProductorId], ct);
        var historial = await db.HistorialProyectos
            .Where(h => h.ProyectoId == id)
            .OrderBy(h => h.Fecha)
            .Select(h => new HistorialProyectoDto(h.EstadoAnterior, h.EstadoNuevo, h.Accion, h.Responsable, h.Motivo, h.Fecha))
            .ToListAsync(ct);
        return new ProyectoEvaluacionDto(ProyectoDto.Desde(proyecto), productor?.NombreCompleto ?? "", productor?.Email ?? "", historial);
    }

    /// <summary>
    /// La decisión del analista se convierte en un comando; el estado del proyecto decide si vale.
    /// Un proyecto que no está en revisión técnica responde 409.
    /// </summary>
    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<ProyectoDto>> Decidir(Guid id, EvaluacionRequest req, CancellationToken ct)
    {
        var texto = req.Observaciones?.Trim() ?? "";
        if (req.Decision != DecisionProyecto.Aprobar && texto.Length == 0)
            return BadRequest(new Respuesta("Escribe las observaciones o el motivo para el productor."));

        var analista = User.Correo();
        ComandoProyecto comando = req.Decision switch
        {
            DecisionProyecto.Observar => new ObservarProyecto(id, analista, texto),
            DecisionProyecto.Rechazar => new RechazarProyecto(id, analista, texto),
            _ => new AprobarYPublicarProyecto(id, analista, texto.Length > 0 ? texto : "Aprobado en evaluación técnica.",
                req.RiesgosCargados, req.TresEscenarios),
        };

        try
        {
            var proyecto = await ciclo.EjecutarAsync(comando, ct);
            return proyecto is null ? NotFound(new Respuesta("No encontramos el proyecto.")) : ProyectoDto.Desde(proyecto);
        }
        catch (TransicionInvalidaException ex)
        {
            return Conflict(new Respuesta(ex.Message));
        }
    }
}
