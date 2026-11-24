using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Models;

namespace P11.Tests.Integration;

/// <summary>RF-11 (catálogo con filtros) y RF-12 (simulador de tres escenarios).</summary>
public class CatalogoTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Al_filtrar_solo_aparecen_los_proyectos_que_cumplen_el_filtro()
    {
        // Una zona única para que los proyectos de otras pruebas no se mezclen.
        var zona = "Zona" + Guid.NewGuid().ToString("N")[..6];
        var engorde = await PasosInversion.ProyectoPublicado(f, "Engorde A", "Engorde", $"Pailón, {zona}", plazo: 10, minimo: 100, riesgo: "Medio");
        var lecheria = await PasosInversion.ProyectoPublicado(f, "Tambo B", "Lecheria", $"Warnes, {zona}", plazo: 12, minimo: 500, riesgo: "Bajo");
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);

        async Task<List<Guid>> Ids(string filtros) =>
            (await inv.GetFromJsonAsync<JsonElement>($"/api/catalogo?zona={zona}{filtros}"))
                .EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();

        Assert.Equal(new[] { engorde, lecheria }.Order(), (await Ids("")).Order());
        Assert.Equal([lecheria], await Ids("&linea=Lecheria"));
        Assert.Equal([engorde], await Ids("&plazoMaximo=10"));
        Assert.Equal([engorde], await Ids("&minimoHasta=200"));
        Assert.Equal([lecheria], await Ids("&riesgo=Bajo"));
        Assert.Empty(await Ids("&linea=Engorde&riesgo=Bajo"));
        // La zona no distingue mayúsculas.
        Assert.Equal(2, (await inv.GetFromJsonAsync<JsonElement>($"/api/catalogo?zona={zona.ToUpperInvariant()}")).GetArrayLength());
    }

    [Fact]
    public async Task Un_proyecto_que_no_esta_publicado_no_aparece_en_el_catalogo()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var creado = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Engorde", nombre = "Borrador oculto", ubicacion = "Montero", montoRequeridoBs = 50_000, montoMinimoBs = 100,
            plazoMeses = 8, cantidadAnimales = 30, pesoInicialKg = 170, pesoMetaKg = 400, costosEstimadosBs = 20_000,
        });
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);

        var catalogo = await inv.GetFromJsonAsync<JsonElement>("/api/catalogo");
        Assert.DoesNotContain(catalogo.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await inv.GetAsync($"/api/catalogo/{id}")).StatusCode);
    }

    [Fact]
    public async Task El_simulador_devuelve_tres_escenarios_con_sus_supuestos_y_la_advertencia()
    {
        var id = await PasosInversion.ProyectoPublicado(f, "Engorde Simulado");
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);

        var r = await inv.PostAsJsonAsync($"/api/catalogo/{id}/simulacion", new { montoBs = 1000 });
        var simulacion = await r.Content.ReadFromJsonAsync<JsonElement>();

        // Supuestos del analista: rendimiento 22 %, costos 8 %, mortalidad 2 %, variación 8 %.
        // valor = monto × (1 + rendimiento − costos − mortalidad)
        var escenarios = simulacion.GetProperty("escenarios").EnumerateArray()
            .Select(e => (e.GetProperty("tipo").GetString(), e.GetProperty("rendimientoPct").GetDecimal(), e.GetProperty("valorEstimadoBs").GetDecimal()))
            .ToList();
        Assert.Equal([("Pesimista", 14m, 1040m), ("Esperado", 22m, 1120m), ("Optimista", 30m, 1200m)], escenarios);
        Assert.Contains("no rentabilidad garantizada", simulacion.GetProperty("advertencia").GetString());
        Assert.False(string.IsNullOrWhiteSpace(simulacion.GetProperty("riesgos").GetString()));
    }

    [Fact]
    public async Task Simular_con_monto_cero_responde_400()
    {
        var id = await PasosInversion.ProyectoPublicado(f, "Engorde Cero");
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);
        Assert.Equal(HttpStatusCode.BadRequest, (await inv.PostAsJsonAsync($"/api/catalogo/{id}/simulacion", new { montoBs = 0 })).StatusCode);
    }

    [Fact]
    public async Task Un_productor_no_entra_al_catalogo()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        Assert.Equal(HttpStatusCode.Forbidden, (await prod.GetAsync("/api/catalogo")).StatusCode);
    }
}
