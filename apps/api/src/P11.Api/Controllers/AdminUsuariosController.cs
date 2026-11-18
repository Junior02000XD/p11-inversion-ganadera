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
public class AdminUsuariosController(
    AppDbContext db, UserManager<Usuario> usuarios, TimeProvider reloj, IAlmacenArchivos almacen) : ControllerBase
{
    [HttpGet]
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

    [HttpPut("{id:guid}/rol")]
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

    [HttpPost("{id:guid}/desbloquear")]
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
    [HttpDelete("{id:guid}")]
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
