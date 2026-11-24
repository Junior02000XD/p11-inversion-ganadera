using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Models;

namespace P11.Tests.Integration;

/// <summary>RF-07 ficha en borrador por línea y RF-05 documentos del predio antes de la revisión técnica.</summary>
public class ProyectosTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RF07_la_ficha_de_lecheria_se_guarda_como_borrador_con_sus_campos()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var r = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Lecheria",
            nombre = "Tambo San Juan",
            ubicacion = "Warnes",
            montoRequeridoBs = 80000,
            montoMinimoBs = 100,
            plazoMeses = 12,
            vacasEnOrdeno = 30,
            litrosPorDia = 450,
            precioLitroBs = 4.2,
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var p = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Borrador", p.GetProperty("estado").GetString());
        Assert.Equal(30, p.GetProperty("vacasEnOrdeno").GetInt32());
        Assert.Equal(JsonValueKind.Null, p.GetProperty("pesoInicialKg").ValueKind);

        var mios = await prod.GetFromJsonAsync<JsonElement>("/api/proyectos");
        Assert.Equal(1, mios.GetArrayLength());
    }

    [Fact]
    public async Task RF07_RF08_ficha_incompleta_no_se_guarda_y_marca_los_campos()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var r = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Engorde",
            nombre = "Sin datos",
            ubicacion = "Pailón",
            montoRequeridoBs = 50000,
            montoMinimoBs = 100,
            plazoMeses = 6,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var errores = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("CantidadAnimales", out _));
        Assert.True(errores.TryGetProperty("PesoMetaKg", out _));
        Assert.Equal(0, (await prod.GetFromJsonAsync<JsonElement>("/api/proyectos")).GetArrayLength());
    }

    [Fact]
    public async Task RF05_sin_documentos_aprobados_el_proyecto_no_pasa_a_revision()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        (await prod.PostAsync("/api/verificacion", Pasos.FormVerificacion(true))).EnsureSuccessStatusCode();
        var creado = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Engorde",
            nombre = "Engorde Pailón",
            ubicacion = "Pailón",
            montoRequeridoBs = 60000,
            montoMinimoBs = 100,
            plazoMeses = 8,
            cantidadAnimales = 30,
            pesoInicialKg = 170,
            pesoMetaKg = 400,
            costosEstimadosBs = 20000,
        });
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Conflict, (await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null)).StatusCode);

        var runsa = await Subir(prod, "Runsa");
        var contrato = await Subir(prod, "ContratoAlquiler");
        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);
        (await analista.PostAsJsonAsync($"/api/analista/documentos/{runsa}/decision", new { aprobar = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null)).StatusCode);

        (await analista.PostAsJsonAsync($"/api/analista/documentos/{contrato}/decision", new { aprobar = true })).EnsureSuccessStatusCode();
        var r = await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("EnRevision", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task RF04_un_productor_no_verificado_no_envia_a_revision()
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var creado = await prod.PostAsJsonAsync("/api/proyectos", new
        {
            linea = "Lecheria",
            nombre = "Tambo",
            ubicacion = "Warnes",
            montoRequeridoBs = 80000,
            montoMinimoBs = 100,
            plazoMeses = 12,
            vacasEnOrdeno = 30,
            litrosPorDia = 450,
            precioLitroBs = 4.2,
        });
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var r = await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Contains("verificada", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());
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
