using System.ComponentModel.DataAnnotations;
using P11.Api.Models;

namespace P11.Api.Dtos;

// Administración: usuarios, roles y bitácora de auditoría (RF-06, RNF-03).

// ---- Entrada ----

/// <summary>Rol nuevo que el administrador le asigna a un usuario.</summary>
public class CambiarRolRequest
{
    [Required]
    public string Rol { get; set; } = "";
}

// ---- Salida ----

/// <summary>Un usuario en la lista del administrador.</summary>
public record UsuarioAdminDto(
    Guid Id,
    string Nombre,
    string Correo,
    string Ci,
    string Rol,
    EstadoVerificacion EstadoVerificacion,
    bool CorreoConfirmado,
    DateTimeOffset? BloqueadoHasta);

/// <summary>Una línea de la bitácora de auditoría (RNF-03): quién cambió qué estado, cuándo y por qué.</summary>
public record EventoBitacoraDto(
    DateTimeOffset Fecha,
    string Modulo,
    Guid ReferenciaId,
    string Referencia,
    string Accion,
    string EstadoAnterior,
    string EstadoNuevo,
    string Responsable,
    string Motivo);
