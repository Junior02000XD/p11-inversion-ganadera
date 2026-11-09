using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Dominio;
using P11.Api.Servicios;
using P11.Api.Servicios.Avisos;

namespace P11.Api.Controllers;

/// <summary>RF-01 registro con OTP (RNF-01) y RF-02 inicio de sesión con bloqueo.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> inicioSesion,
    OtpService otp,
    TokenJwtService tokens,
    ICanalAviso canal,
    TimeProvider reloj,
    ILogger<AuthController> log) : ControllerBase
{
    [HttpPost("registro")]
    public async Task<ActionResult<RegistroResponse>> Registrar(RegistroRequest req, CancellationToken ct)
    {
        if (!Roles.Registrables.Contains(req.Rol))
            return BadRequest(new Respuesta("Solo pueden registrarse inversionistas o productores."));

        var correoUsuario = req.Correo.Trim();
        var ci = req.Ci.Trim();
        if (await usuarios.FindByEmailAsync(correoUsuario) is not null)
            return Conflict(new Respuesta("Ya existe una cuenta con ese correo."));
        if (await usuarios.Users.AnyAsync(u => u.Ci == ci, ct))
            return Conflict(new Respuesta("Ya existe una cuenta con ese CI."));

        var usuario = new Usuario
        {
            NombreCompleto = req.Nombre.Trim(),
            UserName = correoUsuario,
            Email = correoUsuario,
            PhoneNumber = req.Celular.Trim(),
            Ci = ci,
        };
        var creado = await usuarios.CreateAsync(usuario, req.Contrasena);
        if (!creado.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(req.Contrasena)] = creado.Errors.Select(e => e.Description).ToArray(),
            }));
        await usuarios.AddToRoleAsync(usuario, req.Rol);

        var mensaje = "Cuenta creada. Te enviamos un código a tu correo.";
        try
        {
            await otp.GenerarYEnviarAsync(usuario, PropositoOtp.ConfirmarRegistro, ct);
        }
        catch (Exception ex)
        {
            // La cuenta ya existe: el usuario puede pedir el código de nuevo.
            log.LogError(ex, "No se pudo enviar el OTP de registro a {Correo}", usuario.Email);
            mensaje = "Cuenta creada, pero no pudimos enviar el código. Pide que te lo reenviemos.";
        }
        return Created("/api/auth/yo", new RegistroResponse(usuario.Id, usuario.Email!, mensaje));
    }

    [HttpPost("otp/verificar")]
    public async Task<ActionResult<Respuesta>> VerificarOtp(VerificarOtpRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Correo.Trim());
        if (usuario is null) return BadRequest(new Respuesta("El código es incorrecto."));
        if (usuario.EmailConfirmed) return Ok(new Respuesta("La cuenta ya estaba confirmada."));

        var resultado = await otp.ValidarAsync(usuario.Id, PropositoOtp.ConfirmarRegistro, req.Codigo, ct);
        if (resultado != ResultadoOtp.Valido)
        {
            return BadRequest(new Respuesta(resultado switch
            {
                ResultadoOtp.Vencido => "El código venció. Pide uno nuevo.",
                ResultadoOtp.SinIntentos => "Se agotaron los intentos para este código. Pide uno nuevo.",
                ResultadoOtp.Inexistente => "No tienes un código vigente. Pide uno nuevo.",
                _ => "El código es incorrecto.",
            }));
        }

        usuario.EmailConfirmed = true;
        await usuarios.UpdateAsync(usuario);
        return Ok(new Respuesta("Cuenta confirmada. Ya puedes iniciar sesión."));
    }

    [HttpPost("otp/reenviar")]
    public async Task<ActionResult<Respuesta>> ReenviarOtp(ReenviarOtpRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Correo.Trim());
        if (usuario is { EmailConfirmed: false })
            await otp.GenerarYEnviarAsync(usuario, PropositoOtp.ConfirmarRegistro, ct);

        // Misma respuesta exista o no la cuenta, para no revelar qué correos están registrados.
        return Ok(new Respuesta("Si la cuenta existe y no está confirmada, te enviamos un código nuevo."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Correo.Trim());
        if (usuario is null) return Unauthorized(new LoginFallido("Correo o contraseña incorrectos."));

        var estabaBloqueado = await usuarios.IsLockedOutAsync(usuario);
        var resultado = await inicioSesion.CheckPasswordSignInAsync(usuario, req.Contrasena, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            var roles = await usuarios.GetRolesAsync(usuario);
            var token = tokens.Emitir(usuario, roles);
            return Ok(new LoginResponse(token.Token, token.ExpiraEn, UsuarioActual.Desde(usuario, roles)));
        }

        if (resultado.IsNotAllowed)
            return StatusCode(StatusCodes.Status403Forbidden,
                new LoginFallido("Confirma tu cuenta con el código que te enviamos por correo.", RequiereOtp: true));

        if (resultado.IsLockedOut)
        {
            var hasta = await usuarios.GetLockoutEndDateAsync(usuario);
            if (!estabaBloqueado) await AvisarBloqueoAsync(usuario, hasta, ct);
            return StatusCode(StatusCodes.Status423Locked, new LoginFallido(
                $"Tu cuenta quedó bloqueada por intentos fallidos. Vuelve a intentarlo a las {HoraLocal.Formatear(hasta)}.", 0, hasta));
        }

        var restantes = usuarios.Options.Lockout.MaxFailedAccessAttempts - await usuarios.GetAccessFailedCountAsync(usuario);
        return Unauthorized(new LoginFallido("Correo o contraseña incorrectos.", restantes));
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<UsuarioActual>> Yo()
    {
        var usuario = await usuarios.FindByIdAsync(User.IdUsuario().ToString());
        if (usuario is null) return Unauthorized();
        return UsuarioActual.Desde(usuario, await usuarios.GetRolesAsync(usuario));
    }

    private async Task AvisarBloqueoAsync(Usuario usuario, DateTimeOffset? hasta, CancellationToken ct)
    {
        var intentos = usuarios.Options.Lockout.MaxFailedAccessAttempts;
        try
        {
            await new AvisoBloqueo(canal, intentos, reloj.GetUtcNow(), hasta).EnviarAsync(usuario.Email!, ct);
        }
        catch (Exception ex)
        {
            // El bloqueo ya está aplicado; si falla el correo no hay que revertirlo.
            log.LogError(ex, "No se pudo enviar el aviso de bloqueo a {Correo}", usuario.Email);
        }
    }
}
