using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios;

namespace P11.Api.Controllers;

/// <summary>RF-06: el administrador consulta los usuarios, asigna roles, desbloquea y elimina cuentas.</summary>
[ApiController]
[Route("api/admin/usuarios")]
[Authorize(Roles = Roles.Administrador)]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminUsuariosController(
    AppDbContext db, UserManager<Usuario> usuarios, TimeProvider reloj, IAlmacenArchivos almacen) : ControllerBase
{
    /// <summary>Lista las cuentas con su rol, verificación, confirmación de correo y bloqueo vigente.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="200">Usuarios ordenados por fecha de creación. La lista puede estar vacía.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">Solo el administrador puede consultar las cuentas.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioAdminDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioAdminDto>>> Listar(CancellationToken ct)
    {
        var filas = await (
            from u in db.Users
            join ur in db.UserRoles on u.Id equals ur.UserId into rolesDelUsuario
            from ur in rolesDelUsuario.DefaultIfEmpty()
            join r in db.Roles on ur.RoleId equals r.Id into rol
            from r in rol.DefaultIfEmpty()
            orderby u.CreadoEn
            select new { Usuario = u, Rol = r != null ? r.Name : null })
            .ToListAsync(ct);

        var ahora = reloj.GetUtcNow();
        return filas.Select(f => new UsuarioAdminDto(
            f.Usuario.Id, f.Usuario.NombreCompleto, f.Usuario.Email!, f.Usuario.Ci, f.Rol ?? "",
            f.Usuario.EstadoVerificacion, f.Usuario.EmailConfirmed,
            f.Usuario.LockoutEnd > ahora ? f.Usuario.LockoutEnd : null))
            .ToList();
    }

    /// <summary>Cambia el rol de una cuenta. El nuevo rol rige desde su próximo inicio de sesión.</summary>
    /// <param name="id">Identificador GUID del usuario. No puede ser el del administrador autenticado.</param>
    /// <param name="req">Rol que se asignará: Administrador, Analista, Productor o Inversionista.</param>
    /// <response code="200">Rol asignado; devuelve un mensaje de confirmación.</response>
    /// <response code="400">Rol inexistente o intento de cambiar el propio rol (Respuesta). Los datos inválidos generan ValidationProblemDetails.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">Solo el administrador puede cambiar roles.</response>
    /// <response code="404">El usuario no existe.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPut("{id:guid}/rol")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<Respuesta>> CambiarRol(Guid id, CambiarRolRequest req)
    {
        if (!Roles.Todos.Contains(req.Rol)) return BadRequest(new Respuesta("Ese rol no existe."));
        if (id == User.IdUsuario()) return BadRequest(new Respuesta("No puedes cambiar tu propio rol."));

        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound(new Respuesta("No encontramos al usuario."));

        await usuarios.RemoveFromRolesAsync(usuario, await usuarios.GetRolesAsync(usuario));
        await usuarios.AddToRoleAsync(usuario, req.Rol);
        // El token que ya tiene conserva el rol anterior hasta que vence: el cambio rige desde su próximo inicio de sesión.
        return Ok(new Respuesta($"{usuario.NombreCompleto} ahora es {req.Rol}. El cambio rige desde su próximo inicio de sesión."));
    }

    /// <summary>Quita el bloqueo de la cuenta y reinicia el contador de intentos fallidos.</summary>
    /// <param name="id">Identificador GUID del usuario que se quiere desbloquear.</param>
    /// <response code="200">Cuenta desbloqueada; devuelve un mensaje de confirmación.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">Solo el administrador puede desbloquear cuentas.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpPost("{id:guid}/desbloquear")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respuesta>> Desbloquear(Guid id)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound(new Respuesta("No encontramos al usuario."));

        await usuarios.SetLockoutEndDateAsync(usuario, null);
        await usuarios.ResetAccessFailedCountAsync(usuario);
        return Ok(new Respuesta($"Desbloqueamos la cuenta de {usuario.NombreCompleto}."));
    }

    /// <summary>
    /// Elimina una cuenta con sus códigos OTP, su historial de verificación y sus documentos (también los
    /// archivos). Una cuenta con proyectos no se borra (409): la bitácora del proyecto debe quedar (RNF-03);
    /// para esos casos está el bloqueo.
    /// </summary>
    /// <param name="id">Identificador GUID de la cuenta. No puede ser la del administrador autenticado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <response code="204">Cuenta y datos asociados eliminados. La respuesta no tiene cuerpo.</response>
    /// <response code="400">El administrador intentó eliminar su propia cuenta.</response>
    /// <response code="401">Falta el token JWT o no es válido.</response>
    /// <response code="403">Solo el administrador puede eliminar cuentas.</response>
    /// <response code="404">El usuario no existe.</response>
    /// <response code="409">La cuenta tiene proyectos registrados y no se puede eliminar.</response>
    /// <response code="500">Identity no pudo eliminar la cuenta; devuelve el detalle del problema.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        if (id == User.IdUsuario()) return BadRequest(new Respuesta("No puedes eliminar tu propia cuenta."));

        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NotFound(new Respuesta("No encontramos al usuario."));

        if (await db.Proyectos.AnyAsync(p => p.ProductorId == id, ct))
            return Conflict(new Respuesta($"{usuario.NombreCompleto} tiene proyectos registrados y no se puede eliminar. Puedes bloquear la cuenta."));

        var archivos = await db.Documentos.Where(d => d.UsuarioId == id).Select(d => d.ArchivoCifrado).ToListAsync(ct);
        db.CodigosOtp.RemoveRange(db.CodigosOtp.Where(o => o.UsuarioId == id));
        db.HistorialVerificaciones.RemoveRange(db.HistorialVerificaciones.Where(h => h.UsuarioId == id));
        db.Documentos.RemoveRange(db.Documentos.Where(d => d.UsuarioId == id));
        await db.SaveChangesAsync(ct);

        var resultado = await usuarios.DeleteAsync(usuario);
        if (!resultado.Succeeded)
            return Problem(string.Join(" ", resultado.Errors.Select(e => e.Description)));

        foreach (var archivo in archivos) await almacen.EliminarAsync(archivo, ct);
        return NoContent();
    }
}
