namespace P11.Api.Servicios.Inversiones;

/// <summary>
/// Servicio de fondo (RF-15): cada minuto libera las reservas que vencieron sin pago y avisa al inversionista.
/// Aunque no corra, el cupo se calcula bien, porque una reserva vencida ya no cuenta (ver <see cref="Dominio.Inversiones.Inversion.OcupaCupo"/>).
/// </summary>
public class VencimientoReservas(IServiceScopeFactory scopes, ILogger<VencimientoReservas> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await temporizador.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var vencidas = await scope.ServiceProvider.GetRequiredService<GestorInversiones>().VencerReservasAsync(ct);
                if (vencidas > 0) log.LogInformation("Se liberaron {Cantidad} reservas vencidas", vencidas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Falló la revisión de reservas vencidas");
            }
        }
    }
}
