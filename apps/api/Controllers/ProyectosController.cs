using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dtos;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Projects;
using P11.Api.Models.Projects.States;
using P11.Api.Services.Projects;
using P11.Api.Services.Auth;

namespace P11.Api.Controllers;

/// <summary>
/// Recurso principal de la API: la ficha del proyecto ganadero (RF-07). Se crea con el Factory Method según
/// la línea productiva; se edita y se elimina solo mientras está en borrador (lo decide el State).
/// El productor ve y maneja solo sus proyectos; el administrador, todos.
/// </summary>
[ApiController]
[Route("api/proyectos")]
[Authorize(Roles = $"{Roles.Productor},{Roles.Administrador}")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ProyectosController(
    AppDbContext db, SelectorCreadorProyecto selector, RequisitosEnvio requisitos, GestorCicloProyecto ciclo,
    UserManager<Usuario> usuarios) : ControllerBase
{
    private bool EsAdministrador => User.IsInRole(Roles.Administrador);

    /// <summary>Crea la ficha en borrador. Responde 201 con la ruta del proyecto nuevo en <c>Location</c>.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="entrada">Ficha de Engorde o Lecheria. El administrador debe indicar un ProductorId válido.</param>
    /// <response code="201">Borrador creado; devuelve la ficha y su URL en la cabecera Location.</response>
    /// <response code="400">Ficha inválida, campos incompletos o productor inexistente; devuelve errores por campo.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPost]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
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
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Lista de proyectos visibles: los propios del productor o todos para el administrador. Puede estar vacía.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProyectoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProyectoDto>>> Listar(CancellationToken ct)
    {
        var proyectos = await Visibles()
            .OrderByDescending(p => p.CreadoEn)
            .ToListAsync(ct);
        return proyectos.Select(ProyectoDto.Desde).ToList();
    }

    /// <summary>Un proyecto por su id; 404 si no existe o no es del productor.</summary>
    /// <param name="id">Identificador GUID del proyecto que se quiere consultar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Ficha del proyecto solicitado.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="404">El proyecto no existe o no pertenece al productor autenticado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProyectoDto>> Obtener(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        return proyecto is null ? NotFound(new Respuesta("No encontramos el proyecto.")) : ProyectoDto.Desde(proyecto);
    }

    /// <summary>Corrige la ficha. Solo en borrador (409 si no); la línea productiva no cambia (400).</summary>
    /// <param name="id">Identificador GUID del proyecto que se quiere actualizar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="entrada">Ficha corregida, conservando la línea productiva original.</param>
    /// <response code="200">Ficha actualizada del proyecto.</response>
    /// <response code="400">Datos inválidos o intento de cambiar la línea productiva; devuelve errores por campo.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="404">El proyecto no existe o no pertenece al productor autenticado.</response>
    /// <response code="409">El proyecto no está en borrador y no permite editar su ficha.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
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
    /// <param name="id">Identificador GUID del borrador que se quiere eliminar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="204">Borrador e historial eliminados. La respuesta no tiene cuerpo.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="404">El proyecto no existe o no pertenece al productor autenticado.</response>
    /// <response code="409">El estado del proyecto no permite eliminarlo.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
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
    /// <param name="id">Identificador GUID del proyecto cuyo historial se quiere consultar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Cambios de estado ordenados del más antiguo al más reciente. La lista puede estar vacía.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="404">El proyecto no existe o no pertenece al productor autenticado.</response>
    [HttpGet("{id:guid}/historial")]
    [ProducesResponseType<IReadOnlyList<HistorialProyectoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
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
    /// <param name="id">Identificador GUID del borrador que se quiere enviar a revisión.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Proyecto enviado a evaluación técnica con su estado actualizado.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">El usuario no tiene el rol Productor ni Administrador.</response>
    /// <response code="404">El proyecto no existe o no pertenece al productor autenticado.</response>
    /// <response code="409">Faltan requisitos de envío o el estado del proyecto no permite enviarlo a revisión.</response>
    [HttpPost("{id:guid}/enviar-revision")]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProyectoDto>> EnviarARevision(Guid id, CancellationToken ct)
    {
        var proyecto = await BuscarAsync(id, ct);
        if (proyecto is null) return NotFound(new Respuesta("No encontramos el proyecto."));

        var incumplido = await requisitos.RevisarAsync(proyecto, ct);
        if (incumplido is not null) return Conflict(new Respuesta(incumplido));

        try
        {
            // Comando del mismo nombre que esta acción: por eso va con su namespace.
            await ciclo.EjecutarAsync(new Services.Projects.EnviarARevision(proyecto.Id, User.Correo()), ct);
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
