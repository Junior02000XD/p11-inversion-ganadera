using System.ComponentModel.DataAnnotations;
using P11.Api.Models;

namespace P11.Api.Dtos;

// Autenticación: registro, código OTP e inicio de sesión (RF-01, RF-02, RNF-01).

// ---- Entrada ----

/// <summary>Datos del formulario de registro.</summary>
public class RegistroRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string Nombre { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";

    [Required]
    [RegularExpression(@"^\+?[0-9]{7,15}$", ErrorMessage = "El celular no es válido.")]
    public string Celular { get; set; } = "";

    [Required]
    [RegularExpression(@"^[0-9]{5,12}(-[0-9A-Za-z]{1,3})?$", ErrorMessage = "El CI no es válido.")]
    public string Ci { get; set; } = "";

    [Required]
    public string Contrasena { get; set; } = "";

    [Required]
    public string Rol { get; set; } = "";
}

/// <summary>Código OTP que llegó por correo para confirmar la cuenta.</summary>
public class VerificarOtpRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";

    [Required]
    public string Codigo { get; set; } = "";
}

/// <summary>Pide que se envíe un código OTP nuevo.</summary>
public class ReenviarOtpRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";
}

/// <summary>Correo y contraseña para iniciar sesión.</summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";

    [Required]
    public string Contrasena { get; set; } = "";
}

// ---- Salida ----

/// <summary>Cuenta recién creada; falta confirmarla con el OTP.</summary>
public record RegistroResponse(
    Guid UsuarioId,
    string Correo,
    string Mensaje);

/// <summary>Inicio de sesión correcto: el token JWT y quién es el usuario.</summary>
public record LoginResponse(
    string Token,
    DateTimeOffset ExpiraEn,
    UsuarioActual Usuario);

/// <summary>Inicio de sesión rechazado: cuántos intentos quedan o hasta cuándo está bloqueada la cuenta.</summary>
public record LoginFallido(
    string Mensaje,
    int? IntentosRestantes = null,
    DateTimeOffset? BloqueadoHasta = null,
    bool RequiereOtp = false);

/// <summary>El usuario que tiene la sesión abierta.</summary>
public record UsuarioActual(
    Guid Id,
    string Nombre,
    string Correo,
    string Rol,
    EstadoVerificacion EstadoVerificacion,
    bool Consintio)
{
    public static UsuarioActual Desde(Usuario u, IEnumerable<string> roles) => new(
        Id: u.Id,
        Nombre: u.NombreCompleto,
        Correo: u.Email!,
        Rol: roles.FirstOrDefault() ?? "",
        EstadoVerificacion: u.EstadoVerificacion,
        Consintio: u.ConsentimientoDatosEn is not null);
}
