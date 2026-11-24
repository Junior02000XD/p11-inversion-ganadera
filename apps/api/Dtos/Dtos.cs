using System.ComponentModel.DataAnnotations;
using P11.Api.Models;
using P11.Api.Models.Investments;
using P11.Api.Models.Projects;
using P11.Api.Models.Simulation;
using P11.Api.Services.Investments;

namespace P11.Api.Dtos;

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
    int? VacasEnOrdeno, decimal? LitrosPorDia, decimal? PrecioLitroBs, string? Observaciones,
    NivelRiesgo? NivelRiesgo, string? Riesgos, decimal? RendimientoPct, decimal? CostosPct, decimal? MortalidadPct,
    decimal? VariacionPct, DateTimeOffset? PublicadoEn, DateTimeOffset? CierreRondaEn)
{
    public static ProyectoDto Desde(ProyectoGanadero p)
    {
        var engorde = p as ProyectoEngorde;
        var lecheria = p as ProyectoLecheria;
        return new(p.Id, p.Linea, p.Nombre, p.Ubicacion, p.MontoRequeridoBs, p.MontoMinimoBs, p.PlazoMeses, p.Estado, p.CreadoEn,
            engorde?.CantidadAnimales, engorde?.PesoInicialKg, engorde?.PesoMetaKg, engorde?.CostosEstimadosBs,
            lecheria?.VacasEnOrdeno, lecheria?.LitrosPorDia, lecheria?.PrecioLitroBs, p.Observaciones,
            p.NivelRiesgo, p.Riesgos, p.RendimientoPct, p.CostosPct, p.MortalidadPct, p.VariacionPct, p.PublicadoEn, p.CierreRondaEn);
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
    [Range(1, 100_000, ErrorMessage = "Indica la cantidad de animales (entre 1 y 100.000).")] int? CantidadAnimales = null,
    [Range(0.01, 2_000, ErrorMessage = "Indica el peso inicial promedio (hasta 2.000 kg).")] decimal? PesoInicialKg = null,
    [Range(0.01, 2_000, ErrorMessage = "Indica el peso meta (hasta 2.000 kg).")] decimal? PesoMetaKg = null,
    [Range(0.01, 1_000_000_000, ErrorMessage = "Indica los costos estimados.")] decimal? CostosEstimadosBs = null,
    [Range(1, 100_000, ErrorMessage = "Indica las vacas en ordeño (entre 1 y 100.000).")] int? VacasEnOrdeno = null,
    [Range(0.01, 1_000_000, ErrorMessage = "Indica los litros por día.")] decimal? LitrosPorDia = null,
    [Range(0.01, 1_000, ErrorMessage = "Indica el precio del litro (hasta Bs 1.000).")] decimal? PrecioLitroBs = null,
    Guid? ProductorId = null)
{
    public DatosFicha ADatosFicha() => new(Linea!.Value, Nombre, Ubicacion, MontoRequeridoBs, MontoMinimoBs, PlazoMeses,
        CantidadAnimales, PesoInicialKg, PesoMetaKg, CostosEstimadosBs, VacasEnOrdeno, LitrosPorDia, PrecioLitroBs);
}

// ---- Evaluación técnica (RF-09, RF-10) ----

public enum DecisionProyecto { Aprobar, Observar, Rechazar }

/// <summary>
/// Decisión del analista. Observar y rechazar exigen texto. Aprobar exige los riesgos y los supuestos de los
/// tres escenarios (RF-10): rendimiento, costos y mortalidad del escenario esperado, en % del monto invertido
/// para todo el plazo, y cuánto sube o baja el rendimiento en el optimista y el pesimista (variación).
/// </summary>
public record EvaluacionRequest(
    DecisionProyecto Decision,
    string? Observaciones,
    NivelRiesgo? NivelRiesgo = null,
    [StringLength(1000)] string? Riesgos = null,
    decimal? RendimientoPct = null,
    decimal? CostosPct = null,
    decimal? MortalidadPct = null,
    decimal? VariacionPct = null)
{
    public DatosPublicacion ADatosPublicacion() => new(NivelRiesgo, Riesgos, RendimientoPct, CostosPct, MortalidadPct, VariacionPct);
}

public record HistorialProyectoDto(EstadoProyecto EstadoAnterior, EstadoProyecto EstadoNuevo, string Accion, string Responsable, string Motivo, DateTimeOffset Fecha)
{
    public static HistorialProyectoDto Desde(HistorialProyecto h) => new(h.EstadoAnterior, h.EstadoNuevo, h.Accion, h.Responsable, h.Motivo, h.Fecha);
}

public record ProyectoEvaluacionDto(ProyectoDto Proyecto, string Productor, string CorreoProductor, IReadOnlyList<HistorialProyectoDto> Historial);

// ---- Catálogo y simulador (RF-11, RF-12) ----

/// <summary>Un proyecto publicado tal como lo ve el inversionista, con lo recaudado y el cupo libre.</summary>
public record ProyectoCatalogoDto(
    Guid Id, LineaProductiva Linea, string Nombre, string Ubicacion, int PlazoMeses,
    decimal MetaBs, decimal MinimoBs, decimal RecaudadoBs, decimal ReservadoBs, decimal CupoLibreBs, decimal PorcentajeFinanciado,
    NivelRiesgo? NivelRiesgo, string? Riesgos, decimal? RendimientoPct, decimal? CostosPct, decimal? MortalidadPct, decimal? VariacionPct,
    DateTimeOffset? CierreRondaEn)
{
    public static ProyectoCatalogoDto Desde(ProyectoGanadero p, CupoProyecto cupo) => new(
        p.Id, p.Linea, p.Nombre, p.Ubicacion, p.PlazoMeses,
        p.MontoRequeridoBs, p.MontoMinimoBs, cupo.RecaudadoBs, cupo.ReservadoBs, cupo.LibreBs, cupo.PorcentajeFinanciado,
        p.NivelRiesgo, p.Riesgos, p.RendimientoPct, p.CostosPct, p.MortalidadPct, p.VariacionPct, p.CierreRondaEn);
}

public record SimulacionRequest([Range(1, 1_000_000_000, ErrorMessage = "Indica un monto mayor a 0.")] decimal MontoBs);

public record EscenarioDto(TipoEscenario Tipo, decimal RendimientoPct, decimal CostosPct, decimal MortalidadPct, decimal ValorEstimadoBs, decimal ResultadoBs);

/// <summary>RF-12: tres escenarios con sus supuestos y la advertencia de que no son garantizados (RNF-06).</summary>
public record SimulacionDto(Guid ProyectoId, decimal MontoBs, decimal MinimoBs, IReadOnlyList<EscenarioDto> Escenarios, string Advertencia, string? Riesgos);

// ---- Inversión (RF-13 a RF-16) ----

public record ReservaRequest(
    [Required(ErrorMessage = "Indica el proyecto.")] Guid? ProyectoId,
    [Range(0.01, 1_000_000_000, ErrorMessage = "Indica un monto mayor a 0.")] decimal MontoBs);

public record InversionDto(
    Guid Id, Guid ProyectoId, string Proyecto, LineaProductiva Linea, decimal MontoBs, EstadoInversion Estado,
    DateTimeOffset CreadaEn, DateTimeOffset ReservadaHasta,
    string? ContratoVersion, DateTimeOffset? ContratoAceptadoEn, string? ReferenciaPago, DateTimeOffset? PagadaEn,
    decimal? ParticipacionPct, decimal? ParticipacionKg, string? CodigoConstancia)
{
    public static InversionDto Desde(Inversion i, ProyectoGanadero p) => new(
        i.Id, i.ProyectoId, p.Nombre, p.Linea, i.MontoBs, i.Estado, i.CreadaEn, i.ReservadaHasta,
        i.ContratoVersion, i.ContratoAceptadoEn, i.ReferenciaPago, i.PagadaEn, i.ParticipacionPct, i.ParticipacionKg, i.CodigoConstancia);
}

/// <summary>RF-14: el contrato que se acepta, con su versión.</summary>
public record ContratoDto(Guid InversionId, string Version, string Texto, string? Riesgos, decimal MontoBs, DateTimeOffset ReservadaHasta);

public record FirmaContratoRequest(bool AceptaRiesgos, [Required(ErrorMessage = "Ingresa el código que te enviamos.")] string Codigo);

/// <summary>RF-15: lo que va dentro del QR simulado. Vence junto con la reserva.</summary>
public record PagoQrDto(string Referencia, decimal MontoBs, string ContenidoQr, DateTimeOffset VenceEn);

/// <summary>Simula el aviso de la pasarela de pago: aprobado o rechazado.</summary>
public record ConfirmacionPagoRequest(bool Aprobado = true);

/// <summary>RF-16: lo que el inversionista tiene invertido (solo pagos confirmados).</summary>
public record PortafolioDto(decimal TotalInvertidoBs, IReadOnlyList<InversionDto> Participaciones);

/// <summary>RF-16: constancia de participación.</summary>
public record ConstanciaDto(string Codigo, string Inversionista, string Ci, InversionDto Inversion, string UbicacionProyecto, int PlazoMeses);
