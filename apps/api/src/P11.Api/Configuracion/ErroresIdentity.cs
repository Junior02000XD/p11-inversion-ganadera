using Microsoft.AspNetCore.Identity;

namespace P11.Api.Configuracion;

/// <summary>
/// Mensajes de Identity en español (RF-01). Sin esto, las reglas de la contraseña llegan en inglés
/// («Passwords must have at least one uppercase…») a la pantalla de registro.
/// </summary>
public class ErroresIdentity : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe tener al menos un símbolo (por ejemplo . ! # $)." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe tener al menos un número." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe tener al menos una minúscula." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe tener al menos una mayúscula." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Ya existe una cuenta con ese correo." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Ya existe una cuenta con ese correo." };
}
