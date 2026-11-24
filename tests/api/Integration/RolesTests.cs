using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Models;

namespace P11.Tests.Integration;

/// <summary>RF-06: cada rol solo accede a sus funciones.</summary>
public class RolesTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    private static object FichaEngorde => new
    {
        linea = "Engorde",
        nombre = "Engorde Chiquitania",
        ubicacion = "San Ignacio",
        montoRequeridoBs = 100000,
        montoMinimoBs = 100,
        plazoMeses = 8,
        cantidadAnimales = 50,
        pesoInicialKg = 180,
        pesoMetaKg = 420,
        costosEstimadosBs = 30000,
    };

    [Fact]
    public async Task Sin_token_no_hay_acceso()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/auth/yo")).StatusCode);
    }

    [Fact]
    public async Task Un_inversionista_no_puede_registrar_fichas_de_proyecto()
    {
        var inv = await Pasos.UsuarioConfirmado(f, Roles.Inversionista);
        Assert.Equal(HttpStatusCode.Forbidden, (await inv.PostAsJsonAsync("/api/proyectos", FichaEngorde)).StatusCode);
    }

    [Fact]
    public async Task Un_productor_no_accede_a_funciones_de_analista_ni_de_administrador()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        Assert.Equal(HttpStatusCode.Forbidden, (await prod.GetAsync("/api/analista/verificaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await prod.GetAsync("/api/admin/usuarios")).StatusCode);
    }

    [Fact]
    public async Task El_administrador_cambia_el_rol_y_aplica_al_volver_a_entrar()
    {
        var correo = Pasos.CorreoNuevo("cambio");
        await Pasos.UsuarioConfirmado(f, Roles.Inversionista, correo);
        var admin = await Pasos.Autenticado(f, ApiFactory.Admin, ApiFactory.ClaveInterna);

        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/admin/usuarios");
        var id = lista.EnumerateArray().Single(u => u.GetProperty("correo").GetString() == correo).GetProperty("id").GetGuid();
        (await admin.PutAsJsonAsync($"/api/admin/usuarios/{id}/rol", new { rol = Roles.Productor })).EnsureSuccessStatusCode();

        var nuevo = await Pasos.Autenticado(f, correo);
        Assert.Equal(HttpStatusCode.Created, (await nuevo.PostAsJsonAsync("/api/proyectos", FichaEngorde)).StatusCode);
    }

    [Fact]
    public async Task El_administrador_no_puede_cambiar_su_propio_rol()
    {
        var admin = await Pasos.Autenticado(f, ApiFactory.Admin, ApiFactory.ClaveInterna);
        var yo = await admin.GetFromJsonAsync<JsonElement>("/api/auth/yo");
        var r = await admin.PutAsJsonAsync($"/api/admin/usuarios/{yo.GetProperty("id").GetGuid()}/rol", new { rol = Roles.Inversionista });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
