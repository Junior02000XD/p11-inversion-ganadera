using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Dominio.Proyectos.Estados;
using P11.Api.Servicios;
using P11.Api.Servicios.Proyectos;

namespace P11.Api.Controllers;

/// <summary>RF-07: ficha de proyecto en borrador, creada con el Factory Method según la línea productiva.</summary>
[ApiController]
[Route("api/proyectos")]
[Authorize(Roles = Roles.Productor)]
public class ProyectosController(
    AppDbContext db, SelectorCreadorProyecto selector, RequisitosEnvio requisitos, GestorCicloProyecto ciclo) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProyectoDto>> CrearBorrador(DatosFicha datos, CancellationToken ct)
    {
        var creador = selector.Para(datos.Linea);
        var resultado = creador.CrearBorrador(User.IdUsuario(), datos);
        if (resultado.Proyecto is null)
        {
            var errores = resultado.Errores
                .GroupBy(e => e.Campo)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Mensaje).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errores));
        }

        db.Proyectos.Add(resultado.Proyecto);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Proyecto.Id }, ProyectoDto.Desde(resultado.Proyecto));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProyectoDto>>> Mios(CancellationToken ct)
    {
        var productorId = User.IdUsuario();
        var proyectos = await db.Proyectos
            .Where(p => p.ProductorId == productorId)
            .OrderByDescending(p => p.CreadoEn)
            .ToListAsync(ct);
        return proyectos.Select(ProyectoDto.Desde).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProyectoDto>> Obtener(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarPropioAsync(id, ct);
        return proyecto is null ? NotFound() : ProyectoDto.Desde(proyecto);
    }

    /// <summary>
    /// Pasa el borrador a evaluación técnica. Primero la cadena de requisitos (ficha completa, productor
    /// verificado, documentos del predio aprobados: RF-08, RF-04, RF-05); después el comando, y el estado
    /// del proyecto decide si la transición vale («solo un borrador se envía»).
    /// </summary>
    [HttpPost("{id:guid}/enviar-revision")]
    public async Task<ActionResult<ProyectoDto>> EnviarARevision(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarPropioAsync(id, ct);
        if (proyecto is null) return NotFound();

        var incumplido = await requisitos.RevisarAsync(proyecto, ct);
        if (incumplido is not null) return Conflict(new Respuesta(incumplido));

        try
        {
            await ciclo.EjecutarAsync(new Servicios.Proyectos.EnviarARevision(proyecto.Id, User.Correo()), ct);
        }
        catch (TransicionInvalidaException ex)
        {
            return Conflict(new Respuesta(ex.Message));
        }
        return ProyectoDto.Desde(proyecto);
    }

    private Task<ProyectoGanadero?> BuscarPropioAsync(Guid id, CancellationToken ct)
    {
        var productorId = User.IdUsuario();
        return db.Proyectos.FirstOrDefaultAsync(p => p.Id == id && p.ProductorId == productorId, ct);
    }
}
