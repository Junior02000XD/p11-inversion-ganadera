using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using P11.Api.Models;

namespace P11.Tests.Integration;

/// <summary>La misma API de prueba, pero con un reloj que cada prueba adelanta (para vencer reservas).</summary>
public class ApiFactoryConReloj : ApiFactory
{
    public FakeTimeProvider Reloj { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<TimeProvider>();
            s.AddSingleton<TimeProvider>(Reloj);
        });
    }
}

/// <summary>Pasos que repiten las pruebas del Sprint 2: publicar un proyecto e invertir en él.</summary>
public static class PasosInversion
{
    /// <summary>Decisión del analista con riesgos y supuestos completos (RF-10).</summary>
    public static readonly object Publicacion = new
    {
        decision = "Aprobar",
        nivelRiesgo = "Medio",
        riesgos = "Mortalidad, enfermedades o caída del precio de venta.",
        rendimientoPct = 22,
        costosPct = 8,
        mortalidadPct = 2,
        variacionPct = 8,
    };

    /// <summary>Un productor verificado, con documentos aprobados, y su proyecto publicado por el analista.</summary>
    public static async Task<Guid> ProyectoPublicado(ApiFactory f, string nombre, string linea = "Engorde",
        string ubicacion = "San Ignacio de Velasco", decimal meta = 100_000, decimal minimo = 100, int plazo = 10, string riesgo = "Medio")
    {
        var prod = await Pasos.UsuarioConfirmado(f, Roles.Productor);
        (await prod.PostAsync("/api/verificacion", Pasos.FormVerificacion(true))).EnsureSuccessStatusCode();

        var analista = await Pasos.Autenticado(f, ApiFactory.Analista, ApiFactory.ClaveInterna);
        foreach (var tipo in new[] { "Runsa", "TituloPropiedad" })
        {
            var archivo = new ByteArrayContent("%PDF-1.4 documento de prueba"u8.ToArray());
            archivo.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            var form = new MultipartFormDataContent { { new StringContent(tipo), "tipo" }, { archivo, "archivo", $"{tipo}.pdf" } };
            var subido = await prod.PostAsync("/api/productor/documentos", form);
            var doc = (await subido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            (await analista.PostAsJsonAsync($"/api/analista/documentos/{doc}/decision", new { aprobar = true })).EnsureSuccessStatusCode();
        }

        object ficha = linea == "Engorde"
            ? new { linea, nombre, ubicacion, montoRequeridoBs = meta, montoMinimoBs = minimo, plazoMeses = plazo,
                cantidadAnimales = 100, pesoInicialKg = 200, pesoMetaKg = 420, costosEstimadosBs = 30_000 }
            : new { linea, nombre, ubicacion, montoRequeridoBs = meta, montoMinimoBs = minimo, plazoMeses = plazo,
                vacasEnOrdeno = 30, litrosPorDia = 450, precioLitroBs = 4.2m };
        var creado = await prod.PostAsJsonAsync("/api/proyectos", ficha);
        var id = (await creado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await prod.PostAsync($"/api/proyectos/{id}/enviar-revision", null)).EnsureSuccessStatusCode();

        var decision = new
        {
            decision = "Aprobar", nivelRiesgo = riesgo, riesgos = "Mortalidad, enfermedades o caída del precio de venta.",
            rendimientoPct = 22, costosPct = 8, mortalidadPct = 2, variacionPct = 8,
        };
        (await analista.PostAsJsonAsync($"/api/analista/proyectos/{id}/decision", decision)).EnsureSuccessStatusCode();
        return id;
    }

    /// <summary>Un inversionista con el correo confirmado y la identidad verificada (RF-04).</summary>
    public static async Task<(HttpClient cliente, string correo)> InversionistaVerificado(ApiFactory f)
    {
        var correo = Pasos.CorreoNuevo("inversionista");
        var c = await Pasos.UsuarioConfirmado(f, Roles.Inversionista, correo);
        (await c.PostAsync("/api/verificacion", Pasos.FormVerificacion(true))).EnsureSuccessStatusCode();
        return (c, correo);
    }

    /// <summary>Reserva, pide el código y firma el contrato. Devuelve el id de la inversión.</summary>
    public static async Task<Guid> ReservarYFirmar(ApiFactory f, HttpClient c, string correo, Guid proyecto, decimal monto)
    {
        var reserva = await c.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = monto });
        reserva.EnsureSuccessStatusCode();
        var id = (await reserva.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await c.PostAsJsonAsync($"/api/inversiones/{id}/contrato/codigo", new { aceptaRiesgos = true })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/inversiones/{id}/contrato/firmar",
            new { aceptaRiesgos = true, codigo = Pasos.UltimoOtp(f, correo) })).EnsureSuccessStatusCode();
        return id;
    }
}
