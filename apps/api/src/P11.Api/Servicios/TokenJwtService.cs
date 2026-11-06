using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using P11.Api.Configuracion;
using P11.Api.Dominio;

namespace P11.Api.Servicios;

public record TokenEmitido(string Token, DateTimeOffset ExpiraEn);

public class TokenJwtService(IOptions<JwtOptions> opciones, TimeProvider reloj)
{
    public const string ClaimRol = "role";
    public const string ClaimCorreo = JwtRegisteredClaimNames.Email;
    private readonly JwtOptions _o = opciones.Value;

    public TokenEmitido Emitir(Usuario u, IEnumerable<string> roles)
    {
        var expira = reloj.GetUtcNow().AddMinutes(_o.MinutosVigencia);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, u.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimRol, r)));

        var credenciales = new SigningCredentials(ClaveDeFirma(_o), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_o.Emisor, _o.Audiencia, claims,
            notBefore: reloj.GetUtcNow().UtcDateTime, expires: expira.UtcDateTime, signingCredentials: credenciales);
        return new TokenEmitido(new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }

    public static SymmetricSecurityKey ClaveDeFirma(JwtOptions o) => new(Encoding.UTF8.GetBytes(o.ClaveFirma));
}

public static class ClaimsExtensiones
{
    public static Guid IdUsuario(this ClaimsPrincipal p) =>
        Guid.Parse(p.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("El token no trae el id del usuario."));

    public static string Correo(this ClaimsPrincipal p) => p.FindFirstValue(TokenJwtService.ClaimCorreo) ?? "";
}
