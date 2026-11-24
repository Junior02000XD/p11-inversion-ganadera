using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dtos;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Investments;
using P11.Api.Services.Auth;

namespace P11.Api.Controllers;

/// <summary>
/// CU «Consultar portafolio»: las participaciones pagadas del inversionista y su constancia (RF-16).
/// El resto del portafolio (avance y resultados de cada proyecto, RF-26) llega en el Sprint 3.
/// </summary>
[ApiController]
[Route("api/portafolio")]
[Authorize(Roles = $"{Roles.Inversionista},{Roles.Administrador}")]
[Produces("application/json")]
public class PortafolioController(AppDbContext db) : ControllerBase
{
    /// <summary>Mis participaciones (solo inversiones con el pago confirmado) y el total invertido.</summary>
    [HttpGet]
    [ProducesResponseType<PortafolioDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PortafolioDto>> Obtener(CancellationToken ct)
    {
        var yo = User.IdUsuario();
        var filas = await (
            from i in db.Inversiones
            join p in db.Proyectos on i.ProyectoId equals p.Id
            where i.InversionistaId == yo && i.Estado == EstadoInversion.Pagada
            orderby i.PagadaEn descending
            select new { Inversion = i, Proyecto = p })
            .ToListAsync(ct);
        var participaciones = filas.Select(f => InversionDto.Desde(f.Inversion, f.Proyecto)).ToList();
        return new PortafolioDto(participaciones.Sum(p => p.MontoBs), participaciones);
    }

    /// <summary>RF-16: la constancia de una participación pagada; 404 si no es mía o todavía no está pagada.</summary>
    [HttpGet("{inversionId:guid}/constancia")]
    [ProducesResponseType<ConstanciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConstanciaDto>> Constancia(Guid inversionId, CancellationToken ct)
    {
        var yo = User.IdUsuario();
        var inversion = await db.Inversiones.FirstOrDefaultAsync(
            i => i.Id == inversionId && i.InversionistaId == yo && i.Estado == EstadoInversion.Pagada, ct);
        if (inversion is null) return NotFound(new Respuesta("No hay una constancia para esa inversión."));

        var proyecto = await db.Proyectos.FindAsync([inversion.ProyectoId], ct);
        var usuario = await db.Users.FindAsync([yo], ct);
        return new ConstanciaDto(inversion.CodigoConstancia!, usuario!.NombreCompleto, usuario.Ci,
            InversionDto.Desde(inversion, proyecto!), proyecto!.Ubicacion, proyecto.PlazoMeses);
    }
}
