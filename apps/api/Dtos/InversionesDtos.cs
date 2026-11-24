using System.ComponentModel.DataAnnotations;
using P11.Api.Models;
using P11.Api.Models.Investments;
using P11.Api.Models.Projects;

namespace P11.Api.Dtos;

// Inversión: reserva de cupo, contrato, pago QR, portafolio y constancia (RF-13 a RF-16).

// ---- Entrada ----

/// <summary>RF-13: reservar cupo en un proyecto publicado.</summary>
public class ReservaRequest
{
    [Required(ErrorMessage = "Indica el proyecto.")]
    public Guid? ProyectoId { get; set; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "Indica un monto mayor a 0.")]
    public decimal MontoBs { get; set; }
}

/// <summary>RF-14: aceptar el contrato con el código OTP que llegó por correo.</summary>
public class FirmaContratoRequest
{
    public bool AceptaRiesgos { get; set; }

    [Required(ErrorMessage = "Ingresa el código que te enviamos.")]
    public string Codigo { get; set; } = "";
}

/// <summary>Simula el aviso de la pasarela de pago: aprobado o rechazado.</summary>
public class ConfirmacionPagoRequest
{
    public bool Aprobado { get; set; } = true;
}

// ---- Salida ----

/// <summary>Una inversión, desde la reserva hasta el pago y la constancia.</summary>
public record InversionDto(
    // Datos de la inversión
    Guid Id,
    Guid ProyectoId,
    string Proyecto,
    LineaProductiva Linea,
    decimal MontoBs,
    EstadoInversion Estado,
    DateTimeOffset CreadaEn,
    DateTimeOffset ReservadaHasta,

    // Contrato y pago
    string? ContratoVersion,
    DateTimeOffset? ContratoAceptadoEn,
    string? ReferenciaPago,
    DateTimeOffset? PagadaEn,

    // Participación (después del pago)
    decimal? ParticipacionPct,
    decimal? ParticipacionKg,
    string? CodigoConstancia)
{
    public static InversionDto Desde(Inversion i, ProyectoGanadero p) => new(
        Id: i.Id,
        ProyectoId: i.ProyectoId,
        Proyecto: p.Nombre,
        Linea: p.Linea,
        MontoBs: i.MontoBs,
        Estado: i.Estado,
        CreadaEn: i.CreadaEn,
        ReservadaHasta: i.ReservadaHasta,

        ContratoVersion: i.ContratoVersion,
        ContratoAceptadoEn: i.ContratoAceptadoEn,
        ReferenciaPago: i.ReferenciaPago,
        PagadaEn: i.PagadaEn,

        ParticipacionPct: i.ParticipacionPct,
        ParticipacionKg: i.ParticipacionKg,
        CodigoConstancia: i.CodigoConstancia);
}

/// <summary>RF-14: el contrato que se acepta, con su versión.</summary>
public record ContratoDto(
    Guid InversionId,
    string Version,
    string Texto,
    string? Riesgos,
    decimal MontoBs,
    DateTimeOffset ReservadaHasta);

/// <summary>RF-15: lo que va dentro del QR simulado. Vence junto con la reserva.</summary>
public record PagoQrDto(
    string Referencia,
    decimal MontoBs,
    string ContenidoQr,
    DateTimeOffset VenceEn);

/// <summary>RF-16: lo que el inversionista tiene invertido (solo pagos confirmados).</summary>
public record PortafolioDto(
    decimal TotalInvertidoBs,
    IReadOnlyList<InversionDto> Participaciones);

/// <summary>RF-16: constancia de participación.</summary>
public record ConstanciaDto(
    string Codigo,
    string Inversionista,
    string Ci,
    InversionDto Inversion,
    string UbicacionProyecto,
    int PlazoMeses);
