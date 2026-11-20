using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Dominio;

namespace P11.Tests.Integracion;

/// <summary>RF-09 y RF-10 de punta a punta: envío, evaluación del analista, bitácora y avisos.</summary>
public class EvaluacionProyectosTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task El_analista_observa_el_proyecto_vuelve_a_borrador_y_el_productor_recibe_el_aviso()
    {
        var (prod, correoProd, id) = await ProyectoEnRevision("Engorde Montero");
        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);

        // Observer: al entrar a revisión se avisa a los analistas y al productor.
        Assert.Contains(f.Correo.Para(ApiFactory.Analista), c => c.Asunto == "Proyecto por evaluar: «Engorde Montero»");
        Assert.Contains(f.Correo.Para(correoProd), c => c.Asunto.EndsWith("en evaluación técnica"));

        var pendientes = await analista.GetFromJsonAsync<JsonElement>("/api/analista/proyectos");
        Assert.Contains(pendientes.EnumerateArray(), p => p.GetProperty("proyecto").GetProperty("id").GetGuid() == id);

        var sinTexto = await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision", new { decision = "Observar" });
        Assert.Equal(HttpStatusCode.BadRequest, sinTexto.StatusCode);

        var r = await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision",
            new { decision = "Observar", observaciones = "Falta detallar la alimentación." });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var mio = await prod.GetFromJsonAsync<JsonElement>($"/api/proyectos/{id}");
        Assert.Equal("Borrador", mio.GetProperty("estado").GetString());
        Assert.Equal("Falta detallar la alimentación.", mio.GetProperty("observaciones").GetString());
        Assert.Contains(f.Correo.Para(correoProd), c => c.Asunto.EndsWith("tiene observaciones") && c.Cuerpo.Contains("alimentación"));

        // Command: la bitácora tiene las dos acciones con su responsable.
        var detalle = await analista.GetFromJsonAsync<JsonElement>($"/api/analista/proyectos/{id}");
        var acciones = detalle.GetProperty("historial").EnumerateArray()
            .Select(h => (h.GetProperty("accion").GetString(), h.GetProperty("responsable").GetString())).ToList();
        Assert.Equal([("Enviar a revisión", correoProd), ("Observar", ApiFactory.Analista)], acciones);
    }

    [Fact]
    public async Task Solo_se_publica_con_riesgos_y_tres_escenarios_y_despues_no_se_puede_volver_a_decidir()
    {
        var (_, _, id) = await ProyectoEnRevision("Tambo Cotoca");
        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);

        // RF-10: con riesgos pero sin los supuestos de los escenarios, no se publica.
        var incompleto = await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision",
            new { decision = "Aprobar", nivelRiesgo = "Medio", riesgos = "Mortalidad y precio de venta." });
        Assert.Equal(HttpStatusCode.Conflict, incompleto.StatusCode);
        Assert.Contains("tres escenarios", (await incompleto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());

        var ok = await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision", PasosInversion.Publicacion);
        var publicado = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EnFinanciamiento", publicado.GetProperty("estado").GetString());
        Assert.Equal("Medio", publicado.GetProperty("nivelRiesgo").GetString());
        Assert.True(publicado.GetProperty("cierreRondaEn").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddDays(29));

        // State: un proyecto publicado ya no admite «rechazar».
        var otraVez = await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision",
            new { decision = "Rechazar", observaciones = "Cambié de opinión" });
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        Assert.Equal("Un proyecto en financiamiento no se puede rechazar.",
            (await otraVez.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Un_productor_no_puede_evaluar_proyectos()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        Assert.Equal(HttpStatusCode.Forbidden, (await prod.GetAsync("/api/analista/proyectos")).StatusCode);
    }

    /// <summary>Productor verificado, con documentos del predio aprobados y una ficha enviada a revisión.</summary>
    private async Task<(HttpClient prod, string correo, Guid id)> ProyectoEnRevision(string nombre)
    {
        var correo = Pasos.CorreoNuevo("productor");
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor, correo);
        (await prod.PostAsync("/api/verificacion", Pasos.FormVerificacion(true))).EnsureSuccessStatusCode();

        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);
        foreach (var tipo in new[] { "Runsa", "TituloPropiedad" })
        {
            var doc = await Subir(prod, tipo);
            (await analista.PostAsJsonAsync($"/api/analista/documentos/{doc}/decision", new { aprobar = true })).EnsureSuccessStatusCode();
        }

        var creado = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Engorde", nombre, ubicacion = "Montero", montoRequeridoBs = 60000, montoMinimoBs = 100, plazoMeses = 8,
            cantidadAnimales = 30, pesoInicialKg = 170, pesoMetaKg = 400, costosEstimadosBs = 20000,
        });
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null)).EnsureSuccessStatusCode();
        return (prod, correo, id);
    }

    private static async Task<Guid> Subir(HttpClient c, string tipo)
    {
        var archivo = new ByteArrayContent("%PDF-1.4 documento de prueba"u8.ToArray());
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent { { new StringContent(tipo), "tipo" }, { archivo, "archivo", $"{tipo}.pdf" } };
        var r = await c.PostAsync("/api/productor/documentos", form);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
