using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Servicios;

namespace P11.Api.Controllers;

/// <summary>RF-07: ficha de proyecto en borrador, creada con el Factory Method según la línea productiva.</summary>
[ApiController]
[Route("api/proyectos")]
[Authorize(Roles = Roles.Productor)]
public class ProyectosController(AppDbContext db, SelectorCreadorProyecto selector) : ControllerBase
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
    /// Pasa el borrador a evaluación técnica. Exige que el productor esté verificado (RF-04)
    /// y que tenga aprobados el RUNSA y el título o el contrato del predio (RF-05).
    /// </summary>
    [HttpPost("{id:guid}/enviar-revision")]
    public async Task<ActionResult<ProyectoDto>> EnviarARevision(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarPropioAsync(id, ct);
        if (proyecto is null) return NotFound();
        if (proyecto.Estado != EstadoProyecto.Borrador)
            return Conflict(new Respuesta("Solo un borrador se puede enviar a evaluación."));

        var productor = await db.Users.FindAsync([proyecto.ProductorId], ct);
        if (productor?.EstadoVerificacion != EstadoVerificacion.Verificado)
            return Conflict(new Respuesta("Tu identidad todavía no está verificada."));

        var aprobados = await db.Documentos
            .Where(d => d.UsuarioId == proyecto.ProductorId && d.Estado == EstadoDocumento.Aprobado)
            .Select(d => d.Tipo)
            .ToListAsync(ct);
        var tieneRunsa = aprobados.Contains(TipoDocumento.Runsa);
        var tienePredio = aprobados.Contains(TipoDocumento.TituloPropiedad) || aprobados.Contains(TipoDocumento.ContratoAlquiler);
        if (!tieneRunsa || !tienePredio)
            return Conflict(new Respuesta("Faltan documentos aprobados: el RUNSA y el título de propiedad o el contrato de alquiler."));

        proyecto.Estado = EstadoProyecto.EnRevision;
        await db.SaveChangesAsync(ct);
        return ProyectoDto.Desde(proyecto);
    }

    private Task<ProyectoGanadero?> BuscarPropioAsync(Guid id, CancellationToken ct)
    {
        var productorId = User.IdUsuario();
        return db.Proyectos.FirstOrDefaultAsync(p => p.Id == id && p.ProductorId == productorId, ct);
    }
}
