using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace P11.Api.Services.Auth;

/// <summary>Lee del token del usuario conectado su id y su correo: <c>User.IdUsuario()</c>, <c>User.Correo()</c>.</summary>
public static class ClaimsExtensiones
{
    public static Guid IdUsuario(this ClaimsPrincipal p) =>
        Guid.Parse(p.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("El token no trae el id del usuario."));

    public static string Correo(this ClaimsPrincipal p) => p.FindFirstValue(TokenJwtService.ClaimCorreo) ?? "";
}
