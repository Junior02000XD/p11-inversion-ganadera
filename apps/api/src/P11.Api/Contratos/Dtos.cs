using System.ComponentModel.DataAnnotations;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;

namespace P11.Api.Contratos;

/// <summary>Respuesta simple con un texto para mostrar al usuario.</summary>
public record Respuesta(string Mensaje);

// ---- Autenticación (RF-01, RF-02, RNF-01) ----

public record RegistroRequest(
    [Required, StringLength(120, MinimumLength = 3)] string Nombre,
    [Required, EmailAddress] string Correo,
    [Required, RegularExpression(@"^\+?[0-9]{7,15}$", ErrorMessage = "El celular no es válido.")] string Celular,
    [Required, RegularExpression(@"^[0-9]{5,12}(-[0-9A-Za-z]{1,3})?$", ErrorMessage = "El CI no es válido.")] string Ci,
    [Required] string Contrasena,
    [Required] string Rol);

public record RegistroResponse(Guid UsuarioId, string Correo, string Mensaje);

public record VerificarOtpRequest([Required, EmailAddress] string Correo, [Required] string Codigo);
public record ReenviarOtpRequest([Required, EmailAddress] string Correo);

public record LoginRequest([Required, EmailAddress] string Correo, [Required] string Contrasena);
public record LoginResponse(string Token, DateTimeOffset ExpiraEn, UsuarioActual Usuario);
public record LoginFallido(string Mensaje, int? IntentosRestantes = null, DateTimeOffset? BloqueadoHasta = null, bool RequiereOtp = false);

public record UsuarioActual(Guid Id, string Nombre, string Correo, string Rol, EstadoVerificacion EstadoVerificacion, bool Consintio)
{
    public static UsuarioActual Desde(Usuario u, IEnumerable<string> roles) =>
        new(u.Id, u.NombreCompleto, u.Email!, roles.FirstOrDefault() ?? "", u.EstadoVerificacion, u.ConsentimientoDatosEn is not null);
}

// ---- Verificación de identidad y documentos (RF-03, RF-04, RF-05) ----

public record HistorialDto(EstadoVerificacion EstadoAnterior, EstadoVerificacion EstadoNuevo, DateTimeOffset Fecha, string Responsable, string Motivo);
public record EstadoVerificacionDto(EstadoVerificacion Estado, IReadOnlyList<HistorialDto> Historial);
public record ImagenesIlegibles(string Mensaje, IReadOnlyList<TipoDocumento> Ilegibles);

/// <summary>Decisión del analista. Rechazar exige motivo.</summary>
public record DecisionRequest(bool Aprobar, string? Motivo);

public record DocumentoDto(Guid Id, TipoDocumento Tipo, string NombreOriginal, EstadoDocumento Estado, string? Motivo, DateTimeOffset SubidoEn)
{
    public static DocumentoDto Desde(DocumentoArchivo d) => new(d.Id, d.Tipo, d.NombreOriginal, d.Estado, d.Motivo, d.SubidoEn);
}

public record DocumentoRevisionDto(Guid Id, Guid UsuarioId, string Correo, TipoDocumento Tipo, string NombreOriginal, EstadoDocumento Estado, DateTimeOffset SubidoEn);
public record VerificacionPendienteDto(Guid UsuarioId, string Nombre, string Correo, string Ci, string Rol, DateTimeOffset? ConsentimientoEn, IReadOnlyList<DocumentoDto> Documentos);

// ---- Administración (RF-06) ----

public record UsuarioAdminDto(Guid Id, string Nombre, string Correo, string Ci, string Rol, EstadoVerificacion EstadoVerificacion, bool CorreoConfirmado, DateTimeOffset? BloqueadoHasta);
public record CambiarRolRequest([Required] string Rol);

// ---- Proyectos (RF-07) ----

public record ProyectoDto(
    Guid Id, LineaProductiva Linea, string Nombre, string Ubicacion, decimal MontoRequeridoBs, decimal MontoMinimoBs,
    int PlazoMeses, EstadoProyecto Estado, DateTimeOffset CreadoEn,
    int? CantidadAnimales, decimal? PesoInicialKg, decimal? PesoMetaKg, decimal? CostosEstimadosBs,
    int? VacasEnOrdeno, decimal? LitrosPorDia, decimal? PrecioLitroBs)
{
    public static ProyectoDto Desde(ProyectoGanadero p)
    {
        var engorde = p as ProyectoEngorde;
        var lecheria = p as ProyectoLecheria;
        return new(p.Id, p.Linea, p.Nombre, p.Ubicacion, p.MontoRequeridoBs, p.MontoMinimoBs, p.PlazoMeses, p.Estado, p.CreadoEn,
            engorde?.CantidadAnimales, engorde?.PesoInicialKg, engorde?.PesoMetaKg, engorde?.CostosEstimadosBs,
            lecheria?.VacasEnOrdeno, lecheria?.LitrosPorDia, lecheria?.PrecioLitroBs);
    }
}
