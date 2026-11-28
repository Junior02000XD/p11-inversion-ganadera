using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace P11.Tests.Integration;

/// <summary>La API solo acepta tokens firmados con su clave, de su emisor y sin vencer.</summary>
public class TokenJwtTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    private const string Emisor = "p11-api", Audiencia = "p11-clientes";

    private async Task<HttpStatusCode> ConsultarYo(string token)
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await c.GetAsync("/api/auth/yo")).StatusCode;
    }

    /// <summary>Arma un token a mano, con la clave, el emisor y las fechas que se indiquen.</summary>
    private static string Token(string claveBase64, DateTime desde, DateTime hasta, string emisor = Emisor)
    {
        var clave = new SymmetricSecurityKey(Convert.FromBase64String(claveBase64));
        var jwt = new JwtSecurityToken(emisor, Audiencia, [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: desde, expires: hasta, signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    [Fact]
    public async Task El_login_devuelve_el_token_sin_permitir_que_se_guarde_en_cache()
    {
        var correo = Pasos.CorreoNuevo("cache");
        await Pasos.UsuarioConfirmado(f, "Inversionista", correo);
        var r = await Pasos.Login(f.CreateClient(), correo);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.True(r.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Un_token_con_la_firma_alterada_da_401()
    {
        var correo = Pasos.CorreoNuevo("firma");
        await Pasos.UsuarioConfirmado(f, "Inversionista", correo);
        var r = await Pasos.Login(f.CreateClient(), correo);
        var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, await ConsultarYo(token));

        // Se cambia un carácter del medio de la firma (la tercera parte del token).
        var i = token.LastIndexOf('.') + 10;
        var alterado = token[..i] + (token[i] == 'A' ? 'B' : 'A') + token[(i + 1)..];
        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarYo(alterado));
    }

    [Fact]
    public async Task Un_token_firmado_con_otra_clave_da_401()
    {
        var otraClave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var ahora = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarYo(Token(otraClave, ahora, ahora.AddMinutes(10))));
    }

    [Fact]
    public async Task Un_token_vencido_da_401()
    {
        var ahora = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarYo(Token(f.ClaveJwt, ahora.AddMinutes(-20), ahora.AddSeconds(-1))));
    }

    [Fact]
    public async Task Un_token_de_otro_emisor_da_401()
    {
        var ahora = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.Unauthorized, await ConsultarYo(Token(f.ClaveJwt, ahora, ahora.AddMinutes(10), emisor: "otro-sistema")));
    }
}
