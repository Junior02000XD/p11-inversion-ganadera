using Microsoft.AspNetCore.Identity;

namespace P11.Api.Dominio;

/// <summary>Usuario del sistema (clase Usuario del diagrama de clases v5). El rol se guarda en Identity.</summary>
public class Usuario : IdentityUser<Guid>
{
    public string NombreCompleto { get; set; } = "";
    /// <summary>Cédula de identidad; única en el sistema (RF-01).</summary>
    public string Ci { get; set; } = "";
    public EstadoVerificacion EstadoVerificacion { get; set; } = EstadoVerificacion.Pendiente;
    /// <summary>Momento en que aceptó el tratamiento de datos de identidad y biométricos (RNF-07).</summary>
    public DateTimeOffset? ConsentimientoDatosEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
