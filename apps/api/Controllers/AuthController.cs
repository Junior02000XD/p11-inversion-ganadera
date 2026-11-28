using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dtos;
using P11.Api.Models;
using P11.Api.Services;
using P11.Api.Services.Notifications;
using P11.Api.Services.Auth;

namespace P11.Api.Controllers;

/// <summary>RF-01 registro con OTP (RNF-01) y RF-02 inicio de sesión con bloqueo.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> inicioSesion,
    OtpService otp,
    TokenJwtService tokens,
    ICanalAviso canal,
    TimeProvider reloj,
    ILogger<AuthController> log) : ControllerBase
{
    /// <summary>Registra un productor o inversionista y solicita la confirmación de correo mediante OTP.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="req">Datos de la cuenta, contraseña y rol Productor o Inversionista.</param>
    /// <response code="201">Cuenta creada. El mensaje indica si se pudo enviar el OTP; Location apunta a /api/auth/yo.</response>
    /// <response code="400">Datos o contraseña inválidos (ValidationProblemDetails), o rol no registrable (Respuesta).</response>
    /// <response code="409">Ya existe una cuenta con el correo o CI indicado.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPost("registro")]
    [ProducesResponseType<RegistroResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
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

    /// <summary>Confirma el correo con el OTP de registro. Si ya estaba confirmado, informa ese estado.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="req">Correo de la cuenta y código OTP recibido.</param>
    /// <response code="200">Cuenta confirmada o previamente confirmada.</response>
    /// <response code="400">OTP incorrecto, vencido, sin intentos o inexistente (Respuesta). Los datos inválidos generan ValidationProblemDetails.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPost("otp/verificar")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<Respuesta>> VerificarOtp(VerificarOtpRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Correo.Trim());
        if (usuario is null) return BadRequest(new Respuesta("El código es incorrecto."));
        if (usuario.EmailConfirmed) return Ok(new Respuesta("La cuenta ya estaba confirmada."));

        var resultado = await otp.ValidarAsync(usuario.Id, PropositoOtp.ConfirmarRegistro, req.Codigo, ct);
        if (resultado != ResultadoOtp.Valido) return BadRequest(new Respuesta(OtpService.Mensaje(resultado)));

        usuario.EmailConfirmed = true;
        await usuarios.UpdateAsync(usuario);
        return Ok(new Respuesta("Cuenta confirmada. Ya puedes iniciar sesión."));
    }

    /// <summary>Solicita un nuevo OTP sin revelar si el correo está registrado.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="req">Correo al que se reenviará el código si la cuenta existe y aún no está confirmada.</param>
    /// <response code="200">Solicitud procesada; se devuelve el mismo mensaje exista o no la cuenta.</response>
    /// <response code="400">El correo falta o no tiene un formato válido.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    [HttpPost("otp/reenviar")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<Respuesta>> ReenviarOtp(ReenviarOtpRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Correo.Trim());
        if (usuario is { EmailConfirmed: false })
            await otp.GenerarYEnviarAsync(usuario, PropositoOtp.ConfirmarRegistro, ct);

        // Misma respuesta exista o no la cuenta, para no revelar qué correos están registrados.
        return Ok(new Respuesta("Si la cuenta existe y no está confirmada, te enviamos un código nuevo."));
    }

    /// <summary>Inicia sesión y devuelve el token JWT. Los intentos fallidos pueden bloquear la cuenta.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="req">Correo y contraseña de una cuenta confirmada.</param>
    /// <response code="200">Token JWT, fecha de vencimiento y datos del usuario autenticado.</response>
    /// <response code="400">Faltan credenciales o el correo no tiene un formato válido.</response>
    /// <response code="401">Correo o contraseña incorrectos; puede incluir los intentos restantes.</response>
    /// <response code="403">El correo de la cuenta todavía no está confirmado; requiere verificar el OTP.</response>
    /// <response code="415">El cuerpo de la solicitud tiene un tipo de contenido no admitido.</response>
    /// <response code="423">Cuenta bloqueada; incluye la fecha hasta la que dura el bloqueo.</response>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<LoginFallido>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<LoginFallido>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<LoginFallido>(StatusCodes.Status423Locked)]
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
            Response.Headers.CacheControl = "no-store"; // que ningún caché guarde el token
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

    /// <summary>Consulta los datos y el rol del usuario autenticado.</summary>
    /// <response code="200">Datos de la cuenta correspondiente al token JWT.</response>
    /// <response code="401">Falta el token, no es válido o la cuenta asociada ya no existe. Sin cuerpo.</response>
    [Authorize]
    [HttpGet("yo")]
    [ProducesResponseType<UsuarioActual>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
