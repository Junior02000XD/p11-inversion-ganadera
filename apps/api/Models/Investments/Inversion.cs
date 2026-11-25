using P11.Api.Models.Projects;

namespace P11.Api.Models.Investments;

/// <summary>
/// Ciclo de una inversión (RF-13 a RF-16):
/// Reservada → Firmada → Pagada. Si la reserva vence antes de pagar queda Vencida; si la pasarela
/// rechaza el pago, Rechazada. En esos dos casos el cupo vuelve a quedar libre.
/// </summary>
public enum EstadoInversion { Reservada, Firmada, Pagada, Vencida, Rechazada }

/// <summary>Se pidió algo que la inversión no permite en su estado actual (la API responde 409).</summary>
public class ReglaInversionException(string mensaje) : InvalidOperationException(mensaje);

/// <summary>
/// Aporte de un inversionista a un proyecto publicado. Guarda la evidencia de la firma del contrato (RF-14)
/// y, al pagarse, la participación en Bs, kg y % con su constancia (RF-16). No se borra nunca.
/// </summary>
public class Inversion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProyectoId { get; set; }
    public Guid InversionistaId { get; set; }
    public decimal MontoBs { get; set; }
    public EstadoInversion Estado { get; private set; } = EstadoInversion.Reservada;
    public DateTimeOffset CreadaEn { get; set; }
    public DateTimeOffset ReservadaHasta { get; set; }

    // RF-14: quién aceptó, cuándo y qué versión del contrato.
    public string? ContratoVersion { get; private set; }
    public DateTimeOffset? ContratoAceptadoEn { get; private set; }
    public string? ContratoAceptadoPor { get; private set; }

    // RF-15 y RF-16
    public string? ReferenciaPago { get; private set; }
    public DateTimeOffset? PagadaEn { get; private set; }
    public decimal? ParticipacionPct { get; private set; }
    /// <summary>Solo en engorde: los kg de ganado que corresponden a la participación.</summary>
    public decimal? ParticipacionKg { get; private set; }
    public string? CodigoConstancia { get; private set; }

    /// <summary>
    /// Crea la reserva (RF-13), que vence a los <paramref name="minutosReserva"/> minutos (30 en appsettings → Plazos).
    /// Las validaciones de mínimo y cupo las hace quien llama.
    /// </summary>
    public static Inversion Reservar(Guid proyectoId, Guid inversionistaId, decimal montoBs, DateTimeOffset ahora, int minutosReserva) => new()
    {
        ProyectoId = proyectoId,
        InversionistaId = inversionistaId,
        MontoBs = montoBs,
        CreadaEn = ahora,
        ReservadaHasta = ahora.AddMinutes(minutosReserva),
    };

    /// <summary>Ocupa cupo si ya está pagada o si la reserva sigue vigente (RF-13).</summary>
    public bool OcupaCupo(DateTimeOffset ahora) =>
        Estado == EstadoInversion.Pagada || (Estado is EstadoInversion.Reservada or EstadoInversion.Firmada && ahora < ReservadaHasta);

    /// <summary>¿Sigue en curso (reservada o firmada) pero ya pasó la hora límite?</summary>
    public bool Vencio(DateTimeOffset ahora) =>
        Estado is EstadoInversion.Reservada or EstadoInversion.Firmada && ahora >= ReservadaHasta;

    /// <summary>La reserva venció sin pago: el cupo se libera.</summary>
    public void Vencer()
    {
        if (Estado is not (EstadoInversion.Reservada or EstadoInversion.Firmada))
            throw new ReglaInversionException("Solo vence una reserva que todavía no se pagó.");
        Estado = EstadoInversion.Vencida;
    }

    /// <summary>RF-14: registra la aceptación del contrato y los riesgos. Sin esto no se habilita el pago.</summary>
    public void FirmarContrato(string version, string correo, DateTimeOffset ahora)
    {
        ExigirVigente(ahora);
        if (Estado != EstadoInversion.Reservada)
            throw new ReglaInversionException("El contrato de esta inversión ya está firmado.");
        Estado = EstadoInversion.Firmada;
        ContratoVersion = version;
        ContratoAceptadoPor = correo;
        ContratoAceptadoEn = ahora;
    }

    /// <summary>RF-15: genera (una sola vez) la referencia del cobro que va dentro del QR.</summary>
    public string PrepararPago(DateTimeOffset ahora)
    {
        ExigirVigente(ahora);
        if (Estado != EstadoInversion.Firmada)
            throw new ReglaInversionException("Primero acepta el contrato y los riesgos; sin eso no se habilita el pago.");
        return ReferenciaPago ??= $"P11-{Id.ToString("N")[..10].ToUpperInvariant()}";
    }

    /// <summary>RF-15 y RF-16: la pasarela confirmó el pago. Se calcula la participación y se emite la constancia.</summary>
    public void ConfirmarPago(ProyectoGanadero proyecto, DateTimeOffset ahora)
    {
        ExigirVigente(ahora);
        if (Estado != EstadoInversion.Firmada || ReferenciaPago is null)
            throw new ReglaInversionException("Esta inversión no tiene un pago pendiente.");

        Estado = EstadoInversion.Pagada;
        PagadaEn = ahora;
        ParticipacionPct = Math.Round(MontoBs / proyecto.MontoRequeridoBs * 100, 4);
        if (proyecto is ProyectoEngorde engorde)
            ParticipacionKg = Math.Round(ParticipacionPct.Value / 100 * engorde.CantidadAnimales * engorde.PesoInicialKg, 2);
        CodigoConstancia = $"CP-{ahora:yyyyMMdd}-{Id.ToString("N")[..6].ToUpperInvariant()}";
    }

    /// <summary>RF-15: la pasarela rechazó el pago. El cupo se libera.</summary>
    public void RechazarPago()
    {
        if (Estado != EstadoInversion.Firmada)
            throw new ReglaInversionException("Esta inversión no tiene un pago pendiente.");
        Estado = EstadoInversion.Rechazada;
    }

    private void ExigirVigente(DateTimeOffset ahora)
    {
        if (Vencio(ahora))
            throw new ReservaVencidaException();
    }
}

/// <summary>La reserva ya venció: quien llama la marca como vencida, libera el cupo y avisa (RF-15).</summary>
public class ReservaVencidaException() : ReglaInversionException(ReservaVencidaException.Mensaje)
{
    public const string Mensaje = "Tu reserva venció y el cupo se liberó. Puedes volver a reservar si todavía hay cupo.";
}
