using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

/// <summary>
/// Recurso principal de la API: la ficha del proyecto ganadero (RF-07). Se crea con el Factory Method según
/// la línea productiva; se edita y se elimina solo mientras está en borrador (lo decide el State).
/// El productor ve y maneja solo sus proyectos; el administrador, todos.
/// </summary>
[ApiController]
[Route("api/proyectos")]
[Authorize(Roles = $"{Roles.Productor},{Roles.Administrador}")]
public class ProyectosController(
    AppDbContext db, SelectorCreadorProyecto selector, RequisitosEnvio requisitos, GestorCicloProyecto ciclo,
    UserManager<Usuario> usuarios) : ControllerBase
{
    private bool EsAdministrador => User.IsInRole(Roles.Administrador);

    /// <summary>Crea la ficha en borrador. Responde 201 con la ruta del proyecto nuevo en <c>Location</c>.</summary>
    [HttpPost]
    public async Task<ActionResult<ProyectoDto>> CrearBorrador(ProyectoEntradaDto entrada, CancellationToken ct)
    {
        var productorId = User.IdUsuario();
        if (EsAdministrador)
        {
            // El administrador postula en nombre de un productor: la referencia tiene que existir (si no, 400).
            if (entrada.ProductorId is not { } id || !await EsProductorAsync(id))
            {
                ModelState.AddModelError(nameof(ProyectoEntradaDto.ProductorId), "Indica un productor que exista.");
                return ValidationProblem(ModelState);
            }
            productorId = id;
        }

        var datos = entrada.ADatosFicha();
        var resultado = selector.Para(datos.Linea).CrearBorrador(productorId, datos);
        if (resultado.Proyecto is null) return Errores(resultado.Errores);

        db.Proyectos.Add(resultado.Proyecto);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Proyecto.Id }, ProyectoDto.Desde(resultado.Proyecto));
    }

    /// <summary>Lista los proyectos, del más nuevo al más viejo.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProyectoDto>>> Listar(CancellationToken ct)
    {
        var proyectos = await Visibles()
            .OrderByDescending(p => p.CreadoEn)
            .ToListAsync(ct);
        return proyectos.Select(ProyectoDto.Desde).ToList();
    }

    /// <summary>Un proyecto por su id; 404 si no existe o no es del productor.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProyectoDto>> Obtener(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        return proyecto is null ? NotFound(new Respuesta("No encontramos el proyecto.")) : ProyectoDto.Desde(proyecto);
    }

    /// <summary>Corrige la ficha. Solo en borrador (409 si no); la línea productiva no cambia (400).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProyectoDto>> Actualizar(Guid id, ProyectoEntradaDto entrada, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        if (proyecto is null) return NotFound(new Respuesta("No encontramos el proyecto."));

        IReadOnlyList<ErrorCampo> errores;
        try
        {
            errores = proyecto.Actualizar(entrada.ADatosFicha());
        }
        catch (TransicionInvalidaException ex)
        {
            return Conflict(new Respuesta(ex.Message));
        }
        if (errores.Count > 0) return Errores(errores);

        await db.SaveChangesAsync(ct);
        return ProyectoDto.Desde(proyecto);
    }

    /// <summary>Elimina un borrador (204). Lo que ya pasó por evaluación no se borra (409).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        if (proyecto is null) return NotFound(new Respuesta("No encontramos el proyecto."));

        try
        {
            proyecto.EstadoActual.ValidarEliminacion();
        }
        catch (TransicionInvalidaException ex)
        {
            return Conflict(new Respuesta(ex.Message));
        }

        // Un borrador que volvió observado ya tiene historial; la FK es Restrict, así que se borra primero.
        db.HistorialProyectos.RemoveRange(db.HistorialProyectos.Where(h => h.ProyectoId == id));
        db.Proyectos.Remove(proyecto);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Recurso relacionado: la bitácora de cambios de estado del proyecto (envíos, observaciones, decisiones).</summary>
    [HttpGet("{id:guid}/historial")]
    public async Task<ActionResult<IReadOnlyList<HistorialProyectoDto>>> Historial(Guid id, CancellationToken ct)
    {
        if (await BuscarAsync(id, ct) is null) return NotFound(new Respuesta("No encontramos el proyecto."));
        var historial = await db.HistorialProyectos
            .Where(h => h.ProyectoId == id)
            .OrderBy(h => h.Fecha)
            .ToListAsync(ct);
        return historial.Select(HistorialProyectoDto.Desde).ToList();
    }

    /// <summary>
    /// Pasa el borrador a evaluación técnica. Primero la cadena de requisitos (ficha completa, productor
    /// verificado, documentos del predio aprobados: RF-08, RF-04, RF-05); después el comando, y el estado
    /// del proyecto decide si la transición vale («solo un borrador se envía»).
    /// </summary>
    [HttpPost("{id:guid}/enviar-revision")]
    public async Task<ActionResult<ProyectoDto>> EnviarARevision(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        if (proyecto is null) return NotFound(new Respuesta("No encontramos el proyecto."));

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

    /// <summary>El productor ve solo los suyos; el administrador, todos.</summary>
    private IQueryable<ProyectoGanadero> Visibles()
    {
        if (EsAdministrador) return db.Proyectos;
        var productorId = User.IdUsuario();
        return db.Proyectos.Where(p => p.ProductorId == productorId);
    }

    private Task<ProyectoGanadero?> BuscarAsync(Guid id, CancellationToken ct) =>
        Visibles().FirstOrDefaultAsync(p => p.Id == id, ct);

    private async Task<bool> EsProductorAsync(Guid id)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        return usuario is not null && await usuarios.IsInRoleAsync(usuario, Roles.Productor);
    }

    private ActionResult Errores(IReadOnlyList<ErrorCampo> errores) =>
        ValidationProblem(new ValidationProblemDetails(errores
            .GroupBy(e => e.Campo)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Mensaje).ToArray())));
}
