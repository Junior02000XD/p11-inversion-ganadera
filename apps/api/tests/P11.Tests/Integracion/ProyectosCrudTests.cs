using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Contratos;
using P11.Api.Dominio;

namespace P11.Tests.Integracion;

/// <summary>RF-07 y RF-08: contrato HTTP del CRUD de proyectos, pertenencia y restricciones administrativas.</summary>
public class ProyectosCrudTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task POST_valido_devuelve_201_y_Location_del_proyecto_creado()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var ficha = FichaEngorde();
        using var r = await prod.PostAsJsonAsync("/api/proyectos", ficha, Pasos.Json);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var creado = await r.Content.ReadFromJsonAsync<JsonElement>();
        var id = creado.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Borrador", creado.GetProperty("estado").GetString());

        var location = Assert.IsType<Uri>(r.Headers.Location);
        Assert.Equal($"/api/proyectos/{id}", location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString);
        using var consulta = await prod.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var obtenido = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, obtenido.GetProperty("id").GetGuid());
        Assert.Equal(ficha.Nombre, obtenido.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task POST_sin_nombre_ni_linea_devuelve_400_con_ambos_errores()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        using var r = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            ubicacion = "Pailón",
            montoRequeridoBs = 60000,
            montoMinimoBs = 100,
            plazoMeses = 8,
            cantidadAnimales = 30,
            pesoInicialKg = 170,
            pesoMetaKg = 400,
            costosEstimadosBs = 20000,
        });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var errores = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("Nombre", out _));
        Assert.True(errores.TryGetProperty("Linea", out _));
        var proyectos = await prod.GetFromJsonAsync<JsonElement>("/api/proyectos");
        Assert.Empty(proyectos.EnumerateArray());
    }

    [Fact]
    public async Task POST_del_administrador_con_productor_inexistente_devuelve_400()
    {
        using var admin = await Pasos.Autenticado(f, ApiFactory.Admin, ApiFactory.ClaveInterna);
        var ficha = FichaEngorde() with { ProductorId = Guid.NewGuid() };
        using var r = await admin.PostAsJsonAsync("/api/proyectos", ficha, Pasos.Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var errores = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("ProductorId", out _));
    }

    [Fact]
    public async Task GET_de_proyecto_inexistente_devuelve_404()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        using var r = await prod.GetAsync($"/api/proyectos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task PUT_valido_devuelve_200_y_el_nombre_persiste_al_consultar()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var id = await CrearBorrador(prod);
        var ficha = FichaEngorde() with { Nombre = "Engorde Pailón actualizado" };
        using var r = await prod.PutAsJsonAsync($"/api/proyectos/{id}", ficha, Pasos.Json);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var actualizado = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, actualizado.GetProperty("id").GetGuid());
        Assert.Equal(ficha.Nombre, actualizado.GetProperty("nombre").GetString());

        using var consulta = await prod.GetAsync($"/api/proyectos/{id}");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var obtenido = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, obtenido.GetProperty("id").GetGuid());
        Assert.Equal(ficha.Nombre, obtenido.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task PUT_no_permite_cambiar_Engorde_a_Lecheria_y_devuelve_error_Linea()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var id = await CrearBorrador(prod);
        var ficha = FichaEngorde() with
        {
            Linea = LineaProductiva.Lecheria,
            CantidadAnimales = null,
            PesoInicialKg = null,
            PesoMetaKg = null,
            CostosEstimadosBs = null,
            VacasEnOrdeno = 30,
            LitrosPorDia = 450,
            PrecioLitroBs = 4.2m,
        };
        using var r = await prod.PutAsJsonAsync($"/api/proyectos/{id}", ficha, Pasos.Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var errores = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("Linea", out _));
        var obtenido = await prod.GetFromJsonAsync<JsonElement>($"/api/proyectos/{id}");
        Assert.Equal("Engorde", obtenido.GetProperty("linea").GetString());
    }

    [Fact]
    public async Task PUT_de_proyecto_inexistente_devuelve_404()
    {
        using var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        using var r = await prod.PutAsJsonAsync($"/api/proyectos/{Guid.NewGuid()}", FichaEngorde(), Pasos.Json);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    private static ProyectoEntradaDto FichaEngorde() => new(
        LineaProductiva.Engorde, "Engorde Pailón", "Pailón", 60000, 100, 8,
        CantidadAnimales: 30, PesoInicialKg: 170, PesoMetaKg: 400, CostosEstimadosBs: 20000);

    private static async Task<Guid> CrearBorrador(HttpClient prod)
    {
        using var r = await prod.PostAsJsonAsync("/api/proyectos", FichaEngorde(), Pasos.Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var creado = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Borrador", creado.GetProperty("estado").GetString());
        return creado.GetProperty("id").GetGuid();
    }
}
