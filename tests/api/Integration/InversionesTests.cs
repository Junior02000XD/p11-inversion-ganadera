using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace P11.Tests.Integration;

/// <summary>
/// CU «Invertir en un proyecto» de punta a punta: RF-13 (cupo y reserva), RF-14 (contrato con OTP),
/// RF-15 (pago QR simulado, rechazo y vencimiento) y RF-16 (participación y constancia).
/// </summary>
public class InversionesTests(ApiFactoryConReloj f) : IClassFixture<ApiFactoryConReloj>
{
    [Fact]
    public async Task Flujo_completo_reserva_firma_pago_y_participacion_en_el_portafolio()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Chiquitania 2026");
        var (inv, correo) = await PasosInversion.InversionistaVerificado(f);

        // RF-13: reserva por 30 minutos.
        var reserva = await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 1000 });
        Assert.Equal(HttpStatusCode.Created, reserva.StatusCode);
        var inversion = await reserva.Content.ReadFromJsonAsync<JsonElement>();
        var id = inversion.GetProperty("id").GetGuid();
        Assert.Equal("Reservada", inversion.GetProperty("estado").GetString());
        Assert.Equal(f.Reloj.GetUtcNow().AddMinutes(30), inversion.GetProperty("reservadaHasta").GetDateTimeOffset());

        // RF-14: sin aceptar el contrato no se habilita el pago.
        Assert.Equal(HttpStatusCode.Conflict, (await inv.PostAsync($"/api/inversiones/{id}/pago", null)).StatusCode);

        var contrato = await inv.GetFromJsonAsync<JsonElement>($"/api/inversiones/{id}/contrato");
        Assert.Contains("NO está garantizada", contrato.GetProperty("texto").GetString());

        (await inv.PostAsync($"/api/inversiones/{id}/contrato/codigo", null)).EnsureSuccessStatusCode();
        Assert.Contains(f.Correo.Para(correo), c => c.Cuerpo.Contains("firmar el contrato"));
        var codigo = Pasos.UltimoOtp(f, correo);

        var sinAceptar = await inv.PostAsJsonAsync($"/api/inversiones/{id}/contrato/firmar", new { aceptaRiesgos = false, codigo });
        Assert.Equal(HttpStatusCode.BadRequest, sinAceptar.StatusCode);
        var malCodigo = await inv.PostAsJsonAsync($"/api/inversiones/{id}/contrato/firmar",
            new { aceptaRiesgos = true, codigo = codigo == "000000" ? "111111" : "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, malCodigo.StatusCode);

        var firmada = await inv.PostAsJsonAsync($"/api/inversiones/{id}/contrato/firmar", new { aceptaRiesgos = true, codigo });
        var datosFirma = await firmada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Firmada", datosFirma.GetProperty("estado").GetString());
        Assert.Equal("1.0 (2026-11)", datosFirma.GetProperty("contratoVersion").GetString());

        // RF-15: QR simulado y confirmación de la pasarela.
        var qr = await (await inv.PostAsync($"/api/inversiones/{id}/pago", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("P11|", qr.GetProperty("contenidoQr").GetString());
        var pagada = await (await inv.PostAsJsonAsync($"/api/inversiones/{id}/pago/confirmar", new { aprobado = true }))
            .Content.ReadFromJsonAsync<JsonElement>();

        // RF-16: 1.000 de 100.000 = 1 %; en engorde, 1 % de 100 cabezas × 200 kg = 200 kg.
        Assert.Equal("Pagada", pagada.GetProperty("estado").GetString());
        Assert.Equal(1m, pagada.GetProperty("participacionPct").GetDecimal());
        Assert.Equal(200m, pagada.GetProperty("participacionKg").GetDecimal());
        var constancia = pagada.GetProperty("codigoConstancia").GetString()!;
        Assert.Contains(f.Correo.Para(correo), c => c.Asunto == $"Constancia de participación {constancia}");

        var portafolio = await inv.GetFromJsonAsync<JsonElement>("/api/portafolio");
        Assert.Equal(1000m, portafolio.GetProperty("totalInvertidoBs").GetDecimal());
        var doc = await inv.GetFromJsonAsync<JsonElement>($"/api/portafolio/{id}/constancia");
        Assert.Equal(constancia, doc.GetProperty("codigo").GetString());

        var enCatalogo = await inv.GetFromJsonAsync<JsonElement>($"/api/catalogo/{proyecto}");
        Assert.Equal(1000m, enCatalogo.GetProperty("recaudadoBs").GetDecimal());
        Assert.Equal(99_000m, enCatalogo.GetProperty("cupoLibreBs").GetDecimal());
    }

    [Fact]
    public async Task Bajo_el_minimo_responde_400_y_sobre_el_cupo_409_mostrando_el_cupo_disponible()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Cupo", meta: 5_000, minimo: 200);
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);

        var bajo = await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 150 });
        Assert.Equal(HttpStatusCode.BadRequest, bajo.StatusCode);
        var errores = (await bajo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("MontoBs")[0].GetString();
        Assert.Contains("Cupo disponible", errores);

        var alto = await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 6_000 });
        Assert.Equal(HttpStatusCode.Conflict, alto.StatusCode);
        Assert.Contains("quedan Bs", (await alto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Una_reserva_ocupa_cupo_y_al_vencer_lo_libera_y_avisa()
    {
        // Esta prueba adelanta el reloj: usa su propia API para no afectar a las demás.
        using var f = new ApiFactoryConReloj();
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Vencido", meta: 2_000, minimo: 100);
        var (uno, correoUno) = await PasosInversion.InversionistaVerificado(f);
        var (dos, _) = await PasosInversion.InversionistaVerificado(f);

        var reserva = await uno.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 1_500 });
        var id = (await reserva.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await uno.PostAsync($"/api/inversiones/{id}/contrato/codigo", null)).EnsureSuccessStatusCode();

        // Mientras la reserva sigue vigente, solo quedan 500.
        Assert.Equal(HttpStatusCode.Conflict,
            (await dos.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 1_000 })).StatusCode);

        f.Reloj.Advance(TimeSpan.FromMinutes(31));

        // Al vencer: firmar responde 409, la inversión queda Vencida y llega el aviso.
        var tarde = await uno.PostAsJsonAsync($"/api/inversiones/{id}/contrato/firmar",
            new { aceptaRiesgos = true, codigo = Pasos.UltimoOtp(f, correoUno) });
        Assert.Equal(HttpStatusCode.Conflict, tarde.StatusCode);
        Assert.Equal("Vencida", (await uno.GetFromJsonAsync<JsonElement>($"/api/inversiones/{id}")).GetProperty("estado").GetString());
        Assert.Contains(f.Correo.Para(correoUno), c => c.Asunto.Contains("se liberó"));

        // El cupo volvió a quedar libre para otro inversionista.
        Assert.Equal(HttpStatusCode.Created,
            (await dos.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 1_000 })).StatusCode);
    }

    [Fact]
    public async Task Si_la_pasarela_rechaza_el_pago_se_libera_el_cupo_y_se_avisa()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Tambo Rechazo", "Lecheria", meta: 10_000);
        var (inv, correo) = await PasosInversion.InversionistaVerificado(f);
        var id = await PasosInversion.ReservarYFirmar(f, inv, correo, proyecto, 4_000);
        (await inv.PostAsync($"/api/inversiones/{id}/pago", null)).EnsureSuccessStatusCode();

        var rechazada = await inv.PostAsJsonAsync($"/api/inversiones/{id}/pago/confirmar", new { aprobado = false });
        Assert.Equal("Rechazada", (await rechazada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
        Assert.Contains(f.Correo.Para(correo), c => c.Asunto.Contains("se liberó") && c.Cuerpo.Contains("rechazó el pago"));

        var cupo = await inv.GetFromJsonAsync<JsonElement>($"/api/catalogo/{proyecto}");
        Assert.Equal(10_000m, cupo.GetProperty("cupoLibreBs").GetDecimal());
        // Una inversión rechazada no aparece en el portafolio.
        var portafolio = await inv.GetFromJsonAsync<JsonElement>("/api/portafolio");
        Assert.DoesNotContain(portafolio.GetProperty("participaciones").EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Un_inversionista_sin_identidad_verificada_no_puede_invertir()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Sin Verificar");
        var inv = await Pasos.UsuarioConfirmado(f, "Inversionista");

        var r = await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 500 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Contains("no está verificada", (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task No_se_puede_tener_dos_reservas_en_curso_en_el_mismo_proyecto()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Doble");
        var (inv, _) = await PasosInversion.InversionistaVerificado(f);

        Assert.Equal(HttpStatusCode.Created, (await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 500 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await inv.PostAsJsonAsync("/api/inversiones", new { proyectoId = proyecto, montoBs = 500 })).StatusCode);
    }

    [Fact]
    public async Task Nadie_ve_ni_paga_la_inversion_de_otro()
    {
        var proyecto = await PasosInversion.ProyectoPublicado(f, "Engorde Ajeno");
        var (uno, correo) = await PasosInversion.InversionistaVerificado(f);
        var (otro, _) = await PasosInversion.InversionistaVerificado(f);
        var id = await PasosInversion.ReservarYFirmar(f, uno, correo, proyecto, 500);

        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/api/inversiones/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.PostAsync($"/api/inversiones/{id}/pago", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/api/portafolio/{id}/constancia")).StatusCode);
    }
}
