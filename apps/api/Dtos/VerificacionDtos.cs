using P11.Api.Models;

namespace P11.Api.Dtos;

// Verificación de identidad y documentos del predio (RF-03, RF-04, RF-05).

// ---- Entrada ----

/// <summary>Decisión del analista sobre una identidad o un documento. Rechazar exige motivo.</summary>
public class DecisionRequest
{
    public bool Aprobar { get; set; }

    public string? Motivo { get; set; }
}

// ---- Salida ----

/// <summary>Un cambio de estado de la verificación: de qué estado a cuál, quién y por qué.</summary>
public record HistorialDto(
    EstadoVerificacion EstadoAnterior,
    EstadoVerificacion EstadoNuevo,
    DateTimeOffset Fecha,
    string Responsable,
    string Motivo);

/// <summary>Estado actual de la verificación del usuario y su historial.</summary>
public record EstadoVerificacionDto(
    EstadoVerificacion Estado,
    IReadOnlyList<HistorialDto> Historial);

/// <summary>Las imágenes que no se pudieron leer y hay que volver a subir.</summary>
public record ImagenesIlegibles(
    string Mensaje,
    IReadOnlyList<TipoDocumento> Ilegibles);

/// <summary>Un documento subido por el productor (RUNSA o título del predio).</summary>
public record DocumentoDto(
    Guid Id,
    TipoDocumento Tipo,
    string NombreOriginal,
    EstadoDocumento Estado,
    string? Motivo,
    DateTimeOffset SubidoEn)
{
    public static DocumentoDto Desde(DocumentoArchivo d) => new(
        Id: d.Id,
        Tipo: d.Tipo,
        NombreOriginal: d.NombreOriginal,
        Estado: d.Estado,
        Motivo: d.Motivo,
        SubidoEn: d.SubidoEn);
}

/// <summary>Un documento pendiente, tal como lo ve el analista en su bandeja.</summary>
public record DocumentoRevisionDto(
    Guid Id,
    Guid UsuarioId,
    string Correo,
    TipoDocumento Tipo,
    string NombreOriginal,
    EstadoDocumento Estado,
    DateTimeOffset SubidoEn);

/// <summary>Una identidad que espera revisión manual, con sus documentos.</summary>
public record VerificacionPendienteDto(
    Guid UsuarioId,
    string Nombre,
    string Correo,
    string Ci,
    string Rol,
    DateTimeOffset? ConsentimientoEn,
    IReadOnlyList<DocumentoDto> Documentos);
