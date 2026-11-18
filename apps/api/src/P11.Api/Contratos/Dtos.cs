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

/// <summary>Una línea de la bitácora de auditoría (RNF-03): quién cambió qué estado, cuándo y por qué.</summary>
public record EventoBitacoraDto(
    DateTimeOffset Fecha, string Modulo, Guid ReferenciaId, string Referencia, string Accion,
    string EstadoAnterior, string EstadoNuevo, string Responsable, string Motivo);

// ---- Proyectos (RF-07) ----

public record ProyectoDto(
    Guid Id, LineaProductiva Linea, string Nombre, string Ubicacion, decimal MontoRequeridoBs, decimal MontoMinimoBs,
    int PlazoMeses, EstadoProyecto Estado, DateTimeOffset CreadoEn,
    int? CantidadAnimales, decimal? PesoInicialKg, decimal? PesoMetaKg, decimal? CostosEstimadosBs,
    int? VacasEnOrdeno, decimal? LitrosPorDia, decimal? PrecioLitroBs, string? Observaciones)
{
    public static ProyectoDto Desde(ProyectoGanadero p)
    {
        var engorde = p as ProyectoEngorde;
        var lecheria = p as ProyectoLecheria;
        return new(p.Id, p.Linea, p.Nombre, p.Ubicacion, p.MontoRequeridoBs, p.MontoMinimoBs, p.PlazoMeses, p.Estado, p.CreadoEn,
            engorde?.CantidadAnimales, engorde?.PesoInicialKg, engorde?.PesoMetaKg, engorde?.CostosEstimadosBs,
            lecheria?.VacasEnOrdeno, lecheria?.LitrosPorDia, lecheria?.PrecioLitroBs, p.Observaciones);
    }
}

/// <summary>
/// Datos de la ficha que llegan en POST y PUT /api/proyectos. Los atributos validan lo obligatorio y los
/// rangos (400 automático); las reglas que dependen de la línea las valida el dominio. El administrador
/// postula en nombre de un productor y debe indicar <see cref="ProductorId"/>; el productor no lo envía.
/// </summary>
public record ProyectoEntradaDto(
    [Required(ErrorMessage = "Indica la línea productiva (Engorde o Lecheria).")] LineaProductiva? Linea,
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(150)] string Nombre,
    [Required(ErrorMessage = "La ubicación es obligatoria."), StringLength(200)] string Ubicacion,
    [Range(0.01, 1_000_000_000, ErrorMessage = "El monto requerido debe ser mayor a 0.")] decimal MontoRequeridoBs,
    [Range(0.01, 1_000_000_000, ErrorMessage = "El monto mínimo debe ser mayor a 0.")] decimal MontoMinimoBs,
    [Range(1, 120, ErrorMessage = "El plazo debe estar entre 1 y 120 meses.")] int PlazoMeses,
    [Range(1, 100_000)] int? CantidadAnimales = null,
    [Range(0.01, 2_000)] decimal? PesoInicialKg = null,
    [Range(0.01, 2_000)] decimal? PesoMetaKg = null,
    [Range(0.01, 1_000_000_000)] decimal? CostosEstimadosBs = null,
    [Range(1, 100_000)] int? VacasEnOrdeno = null,
    [Range(0.01, 1_000_000)] decimal? LitrosPorDia = null,
    [Range(0.01, 1_000)] decimal? PrecioLitroBs = null,
    Guid? ProductorId = null)
{
    public DatosFicha ADatosFicha() => new(Linea!.Value, Nombre, Ubicacion, MontoRequeridoBs, MontoMinimoBs, PlazoMeses,
        CantidadAnimales, PesoInicialKg, PesoMetaKg, CostosEstimadosBs, VacasEnOrdeno, LitrosPorDia, PrecioLitroBs);
}

// ---- Evaluación técnica (RF-09, RF-10) ----

public enum DecisionProyecto { Aprobar, Observar, Rechazar }

/// <summary>Decisión del analista. Observar y rechazar exigen texto; aprobar exige riesgos y tres escenarios (RF-10).</summary>
public record EvaluacionRequest(DecisionProyecto Decision, string? Observaciones, bool RiesgosCargados = false, bool TresEscenarios = false);

public record HistorialProyectoDto(EstadoProyecto EstadoAnterior, EstadoProyecto EstadoNuevo, string Accion, string Responsable, string Motivo, DateTimeOffset Fecha)
{
    public static HistorialProyectoDto Desde(HistorialProyecto h) => new(h.EstadoAnterior, h.EstadoNuevo, h.Accion, h.Responsable, h.Motivo, h.Fecha);
}

public record ProyectoEvaluacionDto(ProyectoDto Proyecto, string Productor, string CorreoProductor, IReadOnlyList<HistorialProyectoDto> Historial);
