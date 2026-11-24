using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace P11.Tests.Integration;

/// <summary>RF-01 registro con OTP (RNF-01) y RF-02 inicio de sesión con bloqueo al 5.º intento.</summary>
public class AutenticacionTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RF01_registro_valido_crea_la_cuenta_y_envia_un_OTP()
    {
        var c = f.CreateClient();
        var correo = Pasos.CorreoNuevo("inv");
        var r = await Pasos.Registrar(c, correo, "Inversionista");
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Matches(@"^\d{6}$", Pasos.UltimoOtp(f, correo));
    }

    [Fact]
    public async Task RF01_correo_o_CI_ya_registrados_dan_error_y_no_crean_cuenta()
    {
        var c = f.CreateClient();
        var correo = Pasos.CorreoNuevo("dup");
        var ci = Pasos.CiNuevo();
        (await Pasos.Registrar(c, correo, "Productor", ci)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Conflict, (await Pasos.Registrar(c, correo, "Productor")).StatusCode);
        var otroCorreo = Pasos.CorreoNuevo("dup2");
        Assert.Equal(HttpStatusCode.Conflict, (await Pasos.Registrar(c, otroCorreo, "Productor", ci)).StatusCode);
        Assert.Empty(f.Correo.Para(otroCorreo));
    }

    [Fact]
    public async Task RF06_no_se_puede_autoregistrar_un_rol_interno()
    {
        var r = await Pasos.Registrar(f.CreateClient(), Pasos.CorreoNuevo("x"), "Analista");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task RNF01_sin_el_codigo_correcto_la_cuenta_no_se_confirma()
    {
        var c = f.CreateClient();
        var correo = Pasos.CorreoNuevo("otp");
        (await Pasos.Registrar(c, correo, "Inversionista")).EnsureSuccessStatusCode();
        var real = Pasos.UltimoOtp(f, correo);
        var falso = real == "000000" ? "111111" : "000000";

        var r = await c.PostAsJsonAsync("/api/auth/otp/verificar", new { correo, codigo = falso });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var login = await Pasos.Login(c, correo);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.True((await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requiereOtp").GetBoolean());

        (await c.PostAsJsonAsync("/api/auth/otp/verificar", new { correo, codigo = real })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await Pasos.Login(c, correo)).StatusCode);
    }

    [Fact]
    public async Task RF02_login_correcto_devuelve_token_con_el_rol()
    {
        var c = await Pasos.UsuarioConfirmado(f, "Productor");
        var yo = await c.GetFromJsonAsync<JsonElement>("/api/auth/yo");
        Assert.Equal("Productor", yo.GetProperty("rol").GetString());
        Assert.Equal("Pendiente", yo.GetProperty("estadoVerificacion").GetString());
    }

    [Fact]
    public async Task RF02_al_quinto_intento_fallido_bloquea_15_minutos_y_avisa_por_correo()
    {
        var correo = Pasos.CorreoNuevo("bloq");
        await Pasos.UsuarioConfirmado(f, "Inversionista", correo);
        var c = f.CreateClient();

        for (var i = 1; i <= 4; i++)
        {
            var r = await Pasos.Login(c, correo, "incorrecta");
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
            Assert.Equal(5 - i, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("intentosRestantes").GetInt32());
        }
        Assert.DoesNotContain(f.Correo.Para(correo), m => m.Asunto.Contains("Bloqueamos"));

        var quinto = await Pasos.Login(c, correo, "incorrecta");
        Assert.Equal((HttpStatusCode)423, quinto.StatusCode);
        var hasta = (await quinto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("bloqueadoHasta").GetDateTimeOffset();
        Assert.InRange(hasta - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15.1));
        Assert.Single(f.Correo.Para(correo), m => m.Asunto.Contains("Bloqueamos"));

        // Bloqueada, ni la contraseña correcta entra; y el aviso no se repite.
        Assert.Equal((HttpStatusCode)423, (await Pasos.Login(c, correo)).StatusCode);
        Assert.Single(f.Correo.Para(correo), m => m.Asunto.Contains("Bloqueamos"));
    }

    [Fact]
    public async Task RF02_un_login_correcto_reinicia_el_contador_de_intentos()
    {
        var correo = Pasos.CorreoNuevo("reinicio");
        await Pasos.UsuarioConfirmado(f, "Inversionista", correo);
        var c = f.CreateClient();
        for (var i = 0; i < 4; i++) await Pasos.Login(c, correo, "incorrecta");
        Assert.Equal(HttpStatusCode.OK, (await Pasos.Login(c, correo)).StatusCode);
        var r = await Pasos.Login(c, correo, "incorrecta");
        Assert.Equal(4, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("intentosRestantes").GetInt32());
    }
}
