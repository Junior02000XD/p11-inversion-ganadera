using Microsoft.AspNetCore.Identity;
using P11.Api.Dominio;

namespace P11.Api.Datos;

/// <summary>Usuario interno creado al arrancar (Analista y Administrador no se registran solos).</summary>
public class UsuarioSemilla
{
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public string Ci { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Rol { get; set; } = "";
}

public static class SemillaDatos
{
    public static async Task CargarAsync(IServiceProvider sp, IConfiguration config)
    {
        var roles = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var rol in Roles.Todos)
            if (!await roles.RoleExistsAsync(rol))
                await roles.CreateAsync(new IdentityRole<Guid>(rol));

        var usuarios = sp.GetRequiredService<UserManager<Usuario>>();
        foreach (var s in config.GetSection("Semilla:Usuarios").Get<List<UsuarioSemilla>>() ?? [])
        {
            if (await usuarios.FindByEmailAsync(s.Correo) is not null) continue;
            var u = new Usuario
            {
                UserName = s.Correo,
                Email = s.Correo,
                NombreCompleto = s.Nombre,
                EmailConfirmed = true,
                Ci = s.Ci,
                PhoneNumber = s.Celular,
                EstadoVerificacion = EstadoVerificacion.Verificado,
            };
            var r = await usuarios.CreateAsync(u, s.Contrasena);
            if (!r.Succeeded)
                throw new InvalidOperationException($"Semilla {s.Correo}: {string.Join("; ", r.Errors.Select(e => e.Description))}");
            await usuarios.AddToRoleAsync(u, s.Rol);
        }
    }
}
