using P11.Api.Data;
using P11.Api.Models.Projects;

namespace P11.Api.Services.Projects;

/// <summary>
/// Invocador de los comandos (Command) y sujeto de los observadores (Observer) del ciclo de vida del proyecto.
/// Las transiciones las decide el estado del proyecto (State); acá solo se ejecutan, se registran y se avisan.
/// </summary>
public class GestorCicloProyecto(
    AppDbContext db,
    IEnumerable<IObservadorProyecto> observadores,
    TimeProvider reloj,
    ILogger<GestorCicloProyecto> log)
{
    /// <summary>
    /// Ejecuta el comando sobre el proyecto. Si el estado no admite la acción, lanza
    /// <see cref="Models.Projects.States.TransicionInvalidaException"/> y no se guarda nada.
    /// </summary>
    public async Task<ProyectoGanadero?> EjecutarAsync(ComandoProyecto comando, CancellationToken ct = default)
    {
        var proyecto = await db.Proyectos.FindAsync([comando.ProyectoId], ct);
        if (proyecto is null) return null;

        var anterior = proyecto.Estado;
        comando.Ejecutar(proyecto);

        db.HistorialProyectos.Add(new HistorialProyecto
        {
            ProyectoId = proyecto.Id,
            EstadoAnterior = anterior,
            EstadoNuevo = proyecto.Estado,
            Accion = comando.Accion,
            Responsable = comando.Responsable,
            Motivo = comando.Motivo,
            Fecha = reloj.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);

        await NotificarAsync(new CambioEstadoProyecto(proyecto, anterior, comando.Motivo), ct);
        return proyecto;
    }

    // La decisión ya quedó guardada: si un aviso falla, se registra y se sigue con los demás.
    private async Task NotificarAsync(CambioEstadoProyecto cambio, CancellationToken ct)
    {
        foreach (var observador in observadores)
        {
            try
            {
                await observador.EstadoCambiadoAsync(cambio, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "{Observador} no pudo procesar el cambio de {Proyecto} a {Estado}",
                    observador.GetType().Name, cambio.Proyecto.Id, cambio.Proyecto.Estado);
            }
        }
    }
}
