using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using P11.Api.Dominio;
using P11.Tests.Integracion;

namespace P11.Tests.Patrones;

/// <summary>Proxy de protección: un documento lo abre su dueño o un analista; nadie más.</summary>
public class ProxyTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    private static async Task<Guid> SubirRunsa(HttpClient productor, byte[] contenido)
    {
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent { { new StringContent("Runsa"), "tipo" }, { archivo, "archivo", "runsa.png" } };
        var r = await productor.PostAsync("/api/productor/documentos", form);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task El_dueno_abre_su_documento_descifrado()
    {
        var productor = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var png = Pasos.Png();
        var id = await SubirRunsa(productor, png);

        var r = await productor.GetAsync($"/api/productor/documentos/{id}/archivo");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(png, await r.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Otro_productor_no_puede_abrirlo_ni_saber_que_existe()
    {
        var dueno = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var id = await SubirRunsa(dueno, Pasos.Png());
        var otro = await Pasos.UsuarioConfirmado(f, Roles.Productor);

        var r = await otro.GetAsync($"/api/productor/documentos/{id}/archivo");

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task El_analista_abre_los_documentos_que_revisa()
    {
        var productor = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var png = Pasos.Png();
        var id = await SubirRunsa(productor, png);
        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);

        var r = await analista.GetAsync($"/api/analista/archivos/{id}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(png, await r.Content.ReadAsByteArrayAsync());
    }
}
