using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios;

namespace P11.Tests.Integracion;

/// <summary>RF-03/04 verificación de identidad simulada, RNF-07 consentimiento, RNF-02 cifrado.</summary>
public class VerificacionTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RNF07_sin_consentimiento_no_se_carga_ni_el_CI_ni_la_selfie()
    {
        var c = await Pasos.UsuarioConfirmado(f, Roles.Inversionista);
        var r = await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(consentimiento: false));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        var yo = await c.GetFromJsonAsync<JsonElement>("/api/auth/yo");
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Documentos.Where(d => d.UsuarioId == yo.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task RF03_imagen_ilegible_pide_fotos_nuevas()
    {
        var c = await Pasos.UsuarioConfirmado(f, Roles.Inversionista);
        var r = await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(true, ciDorso: "no es una imagen"u8.ToArray()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        var cuerpo = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["CiDorso"], cuerpo.GetProperty("ilegibles").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task RF03_coincidencia_sobre_el_umbral_verifica_y_guarda_historial()
    {
        var c = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        var r = await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(true));
        r.EnsureSuccessStatusCode();
        var estado = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Verificado", estado.GetProperty("estado").GetString());
        var h = estado.GetProperty("historial").EnumerateArray().Single();
        Assert.Equal("sistema", h.GetProperty("responsable").GetString());
        Assert.False(string.IsNullOrWhiteSpace(h.GetProperty("motivo").GetString()));
    }

    [Fact]
    public async Task RF03_RF04_bajo_el_umbral_pasa_a_revision_y_el_analista_decide_con_motivo()
    {
        var correo = Pasos.CorreoNuevo("revision");
        var c = await Pasos.UsuarioConfirmado(f, Roles.Inversionista, correo);
        var r = await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(true, nombreSelfie: "selfie-revision.png"));
        Assert.Equal("EnRevision", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());

        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);
        var cola = await analista.GetFromJsonAsync<JsonElement>("/api/analista/verificaciones");
        var pendiente = cola.EnumerateArray().Single(v => v.GetProperty("correo").GetString() == correo);
        Assert.Equal(3, pendiente.GetProperty("documentos").GetArrayLength());
        var id = pendiente.GetProperty("usuarioId").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await analista.PostAsJsonAsync($"/api/analista/verificaciones/{id}/decision", new { aprobar = false })).StatusCode);
        (await analista.PostAsJsonAsync($"/api/analista/verificaciones/{id}/decision", new { aprobar = true, motivo = "Rostro coincide al revisar" }))
            .EnsureSuccessStatusCode();

        var estado = await c.GetFromJsonAsync<JsonElement>("/api/verificacion/estado");
        Assert.Equal("Verificado", estado.GetProperty("estado").GetString());
        var historial = estado.GetProperty("historial").EnumerateArray().ToList();
        Assert.Equal(2, historial.Count);
        Assert.Equal(ApiFactory.Analista, historial[1].GetProperty("responsable").GetString());
        Assert.Equal("Rostro coincide al revisar", historial[1].GetProperty("motivo").GetString());
        Assert.Contains(f.Correo.Para(correo), m => m.Asunto.EndsWith(": Verificado"));
    }

    [Fact]
    public async Task RNF02_el_archivo_en_disco_esta_cifrado_y_no_se_abre_sin_la_clave()
    {
        var c = await Pasos.UsuarioConfirmado(f, Roles.Inversionista);
        (await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(true))).EnsureSuccessStatusCode();
        var yo = await c.GetFromJsonAsync<JsonElement>("/api/auth/yo");

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = db.Documentos.First(d => d.UsuarioId == yo.GetProperty("id").GetGuid() && d.Tipo == TipoDocumento.Selfie);
        var enDisco = await File.ReadAllBytesAsync(Path.Combine(f.CarpetaArchivos, doc.ArchivoCifrado));
        var original = Pasos.Png();

        Assert.NotEqual(original, enDisco);
        Assert.False(ValidadorArchivos.EsImagenLegible(enDisco, 1), "En disco no debe verse la cabecera PNG.");

        // Con la clave del sistema se recupera exacto; con otra clave, falla.
        var almacen = scope.ServiceProvider.GetRequiredService<IAlmacenArchivos>();
        Assert.Equal(original, await almacen.LeerAsync(doc.ArchivoCifrado));
        var opciones = Options.Create(new ArchivosOptions
        {
            Carpeta = f.CarpetaArchivos,
            ClaveBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });
        var otraClave = new AlmacenCifrado(new AlmacenDisco(opciones, scope.ServiceProvider.GetRequiredService<IHostEnvironment>()), opciones);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => otraClave.LeerAsync(doc.ArchivoCifrado));
    }
}
