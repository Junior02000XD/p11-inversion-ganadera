using Microsoft.EntityFrameworkCore;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Projects;
using P11.Api.Services.Projects;

namespace P11.Tests.Patterns;

/// <summary>Chain of Responsibility (RF-04, RF-05, RF-08): el primer requisito que falla corta la cadena.</summary>
public class ChainOfResponsibilityTests
{
    // Eslabón de prueba: anota que lo consultaron y responde lo que se le indique.
    private class Eslabon(string nombre, List<string> llamados, string? falla = null) : RequisitoEnvio
    {
        protected override Task<string?> IncumplidoAsync(ProyectoGanadero proyecto, CancellationToken ct)
        {
            llamados.Add(nombre);
            return Task.FromResult(falla);
        }
    }

    [Fact]
    public async Task Si_todos_se_cumplen_se_recorre_la_cadena_entera()
    {
        var llamados = new List<string>();
        var cadena = new Eslabon("a", llamados);
        cadena.Luego(new Eslabon("b", llamados)).Luego(new Eslabon("c", llamados));

        Assert.Null(await cadena.RevisarAsync(Fichas.Engorde(), default));
        Assert.Equal(["a", "b", "c"], llamados);
    }

    [Fact]
    public async Task El_primero_que_falla_responde_y_los_siguientes_no_se_consultan()
    {
        var llamados = new List<string>();
        var cadena = new Eslabon("a", llamados);
        cadena.Luego(new Eslabon("b", llamados, falla: "falta b")).Luego(new Eslabon("c", llamados));

        Assert.Equal("falta b", await cadena.RevisarAsync(Fichas.Engorde(), default));
        Assert.Equal(["a", "b"], llamados);
    }

    [Fact]
    public async Task La_cadena_real_pide_verificacion_antes_que_documentos()
    {
        using var db = BaseEnMemoria();
        var productor = new Usuario { UserName = "p@test.bo", Email = "p@test.bo", Ci = "1", NombreCompleto = "P" };
        db.Users.Add(productor);
        await db.SaveChangesAsync();
        var proyecto = Fichas.Engorde(productor.Id);
        var requisitos = new RequisitosEnvio(db);

        Assert.Equal("Tu identidad todavía no está verificada.", await requisitos.RevisarAsync(proyecto));

        productor.EstadoVerificacion = EstadoVerificacion.Verificado;
        await db.SaveChangesAsync();
        Assert.StartsWith("Faltan documentos aprobados", await requisitos.RevisarAsync(proyecto));

        db.Documentos.AddRange(
            new DocumentoArchivo { UsuarioId = productor.Id, Tipo = TipoDocumento.Runsa, Estado = EstadoDocumento.Aprobado },
            new DocumentoArchivo { UsuarioId = productor.Id, Tipo = TipoDocumento.TituloPropiedad, Estado = EstadoDocumento.Aprobado });
        await db.SaveChangesAsync();
        Assert.Null(await requisitos.RevisarAsync(proyecto));
    }

    internal static AppDbContext BaseEnMemoria() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("cadena-" + Guid.NewGuid()).Options);
}
