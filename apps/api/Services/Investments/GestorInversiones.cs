using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;
using P11.Api.Data;
using P11.Api.Models.Investments;
using P11.Api.Services.Notifications;

namespace P11.Api.Services.Investments;

/// <summary>Cuánto dinero tiene un proyecto: la meta, lo pagado, lo reservado y lo que queda libre (RF-13).</summary>
public record CupoProyecto(decimal MetaBs, decimal RecaudadoBs, decimal ReservadoBs)
{
    public decimal LibreBs => Math.Max(0, MetaBs - RecaudadoBs - ReservadoBs);
    public decimal PorcentajeFinanciado => MetaBs == 0 ? 0 : Math.Round(RecaudadoBs / MetaBs * 100, 2);
}

/// <summary>
/// Reglas de inversión que necesitan la base: cuánto cupo queda y qué hacer con las reservas vencidas.
/// Las reglas de una inversión sola (firmar, pagar) están en la entidad <see cref="Inversion"/>.
/// </summary>
public class GestorInversiones(
    AppDbContext db, ICanalAviso canal, TimeProvider reloj, IOptions<PlazosOptions> plazos, ILogger<GestorInversiones> log)
{
    public DateTimeOffset Ahora => reloj.GetUtcNow();

    /// <summary>RF-13: cuánto dura la reserva del cupo (appsettings → Plazos).</summary>
    public int MinutosReserva => plazos.Value.MinutosReserva;

    /// <summary>El cupo de varios proyectos de una vez (el catálogo los muestra todos juntos).</summary>
    public async Task<Dictionary<Guid, CupoProyecto>> CuposAsync(IReadOnlyCollection<(Guid Id, decimal MetaBs)> proyectos, CancellationToken ct)
    {
        var ids = proyectos.Select(p => p.Id).ToList();
        var inversiones = await db.Inversiones.Where(i => ids.Contains(i.ProyectoId)).ToListAsync(ct);
        var ahora = Ahora;
        return proyectos.ToDictionary(p => p.Id, p =>
        {
            var delProyecto = inversiones.Where(i => i.ProyectoId == p.Id && i.OcupaCupo(ahora)).ToList();
            return new CupoProyecto(
                p.MetaBs,
                delProyecto.Where(i => i.Estado == EstadoInversion.Pagada).Sum(i => i.MontoBs),
                delProyecto.Where(i => i.Estado != EstadoInversion.Pagada).Sum(i => i.MontoBs));
        });
    }

    public async Task<CupoProyecto> CupoAsync(Guid proyectoId, decimal metaBs, CancellationToken ct) =>
        (await CuposAsync([(proyectoId, metaBs)], ct))[proyectoId];

    /// <summary>
    /// Marca como vencida una inversión cuya reserva ya pasó y avisa al inversionista (RF-15).
    /// Devuelve true si la venció. Se llama al tocar la inversión y desde el servicio de fondo.
    /// </summary>
    public async Task<bool> VencerSiCorrespondeAsync(Inversion inversion, CancellationToken ct)
    {
        if (!inversion.Vencio(Ahora)) return false;
        inversion.Vencer();
        await db.SaveChangesAsync(ct);
        await AvisarLiberacionAsync(inversion, $"pasaron los {MinutosReserva} minutos de la reserva sin que se confirmara el pago.", ct);
        return true;
    }

    /// <summary>Recorre todas las reservas vencidas (lo usa <see cref="VencimientoReservas"/> cada minuto).</summary>
    public async Task<int> VencerReservasAsync(CancellationToken ct)
    {
        var ahora = Ahora;
        var enCurso = await db.Inversiones
            .Where(i => (i.Estado == EstadoInversion.Reservada || i.Estado == EstadoInversion.Firmada) && i.ReservadaHasta <= ahora)
            .ToListAsync(ct);
        foreach (var inversion in enCurso)
            await VencerSiCorrespondeAsync(inversion, ct);
        return enCurso.Count;
    }

    /// <summary>Avisa que el cupo se liberó. Si el correo falla, la inversión ya quedó guardada: solo se registra.</summary>
    public async Task AvisarLiberacionAsync(Inversion inversion, string motivo, CancellationToken ct)
    {
        try
        {
            var (correo, proyecto) = await DestinoAsync(inversion, ct);
            if (correo is not null) await new AvisoCupoLiberado(canal, proyecto, inversion.MontoBs, motivo).EnviarAsync(correo, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo avisar la liberación de la inversión {Inversion}", inversion.Id);
        }
    }

    /// <summary>RF-16: aviso con la constancia de participación.</summary>
    public async Task AvisarParticipacionAsync(Inversion inversion, CancellationToken ct)
    {
        try
        {
            var (correo, proyecto) = await DestinoAsync(inversion, ct);
            if (correo is not null)
                await new AvisoParticipacion(canal, proyecto, inversion.MontoBs, inversion.ParticipacionPct ?? 0,
                    inversion.ParticipacionKg, inversion.CodigoConstancia ?? "").EnviarAsync(correo, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo enviar la constancia de la inversión {Inversion}", inversion.Id);
        }
    }

    private async Task<(string? Correo, string Proyecto)> DestinoAsync(Inversion inversion, CancellationToken ct)
    {
        var usuario = await db.Users.FindAsync([inversion.InversionistaId], ct);
        var proyecto = await db.Proyectos.FindAsync([inversion.ProyectoId], ct);
        return (usuario?.Email, proyecto?.Nombre ?? "el proyecto");
    }
}
