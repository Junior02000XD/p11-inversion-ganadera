using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using P11.Api.Configuration;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Services;
using P11.Api.Services.Auth;

namespace P11.Tests.Integration;

/// <summary>RNF-01: vencimiento, intentos máximos y anulación del código anterior, con reloj controlado.</summary>
public class OtpServiceTests
{
    private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CorreoEnMemoria _correo = new();
    private readonly AppDbContext _db = new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("otp-" + Guid.NewGuid()).Options);
    private readonly Usuario _u = new() { Id = Guid.NewGuid(), Email = "otp@test.bo", UserName = "otp@test.bo", Ci = "123" };
    private OtpService Servicio => new(_db, _correo, Options.Create(new OtpOptions()), _reloj);

    private string UltimoCodigo() => System.Text.RegularExpressions.Regex.Match(_correo.Enviados.Last().Asunto, @"\d{6}").Value;

    [Fact]
    public async Task Codigo_vencido_se_rechaza()
    {
        await Servicio.GenerarYEnviarAsync(_u, PropositoOtp.ConfirmarRegistro);
        var codigo = UltimoCodigo();
        _reloj.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(ResultadoOtp.Vencido, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, codigo));
    }

    [Fact]
    public async Task Al_tercer_error_se_agotan_los_intentos_aunque_despues_llegue_el_correcto()
    {
        await Servicio.GenerarYEnviarAsync(_u, PropositoOtp.ConfirmarRegistro);
        var codigo = UltimoCodigo();
        var falso = codigo == "000000" ? "111111" : "000000";
        Assert.Equal(ResultadoOtp.Incorrecto, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, falso));
        Assert.Equal(ResultadoOtp.Incorrecto, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, falso));
        Assert.Equal(ResultadoOtp.SinIntentos, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, falso));
        Assert.Equal(ResultadoOtp.SinIntentos, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, codigo));
    }

    [Fact]
    public async Task Un_codigo_nuevo_anula_el_anterior_y_cada_codigo_sirve_una_sola_vez()
    {
        await Servicio.GenerarYEnviarAsync(_u, PropositoOtp.ConfirmarRegistro);
        var viejo = UltimoCodigo();
        await Servicio.GenerarYEnviarAsync(_u, PropositoOtp.ConfirmarRegistro);
        var nuevo = UltimoCodigo();

        if (viejo != nuevo)
            Assert.Equal(ResultadoOtp.Incorrecto, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, viejo));
        Assert.Equal(ResultadoOtp.Valido, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, nuevo));
        Assert.Equal(ResultadoOtp.Inexistente, await Servicio.ValidarAsync(_u.Id, PropositoOtp.ConfirmarRegistro, nuevo));
    }

    [Fact]
    public async Task En_la_base_solo_queda_el_hash_del_codigo()
    {
        await Servicio.GenerarYEnviarAsync(_u, PropositoOtp.ConfirmarRegistro);
        var guardado = await _db.CodigosOtp.SingleAsync();
        Assert.DoesNotContain(UltimoCodigo(), guardado.CodigoHash);
        Assert.Equal(64, guardado.CodigoHash.Length);
    }
}
