using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using P11.Api.Models;
using P11.Api.Models.Projects;

namespace P11.Api.Data;

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

        // Los proyectos de demostración se crean una sola vez y solo si existe el productor semilla.
        var productor = await usuarios.FindByEmailAsync("productor@p11.local");
        if (productor is null) return;

        var db = sp.GetRequiredService<AppDbContext>();
        if (await db.Proyectos.AnyAsync()) return;

        var selector = sp.GetRequiredService<SelectorCreadorProyecto>();
        var fichas = new[]
        {
            new DatosFicha(LineaProductiva.Engorde, "Engorde Chiquitania 2026", "San Ignacio de Velasco",
                MontoRequeridoBs: 500_000m, MontoMinimoBs: 1_000m, PlazoMeses: 12,
                CantidadAnimales: 100, PesoInicialKg: 180m, PesoMetaKg: 350m, CostosEstimadosBs: 400_000m),
            new DatosFicha(LineaProductiva.Lecheria, "Tambo Warnes", "Warnes",
                MontoRequeridoBs: 300_000m, MontoMinimoBs: 1_000m, PlazoMeses: 18,
                VacasEnOrdeno: 40, LitrosPorDia: 500m, PrecioLitroBs: 3m),
        };

        foreach (var ficha in fichas)
        {
            var resultado = selector.Para(ficha.Linea).CrearBorrador(productor.Id, ficha);
            if (!resultado.Exitoso)
                throw new InvalidOperationException($"Proyecto semilla {ficha.Nombre}: {string.Join("; ", resultado.Errores.Select(e => e.Mensaje))}");
            db.Proyectos.Add(resultado.Proyecto!);
        }
        await db.SaveChangesAsync();
    }
}
