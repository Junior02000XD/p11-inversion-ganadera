using Microsoft.AspNetCore.Identity;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Projects;
using P11.Api.Services.Notifications;

namespace P11.Api.Services.Projects;

public record CambioEstadoProyecto(ProyectoGanadero Proyecto, EstadoProyecto Anterior, string Motivo);

/// <summary>
/// Observer (GoF): se entera de cada cambio de estado del proyecto. El sujeto (<see cref="GestorCicloProyecto"/>)
/// no conoce a los observadores concretos: los recibe del contenedor de dependencias.
/// </summary>
public interface IObservadorProyecto
{
    Task EstadoCambiadoAsync(CambioEstadoProyecto cambio, CancellationToken ct);
}

/// <summary>RF-09: el productor se entera de cada decisión sobre su proyecto (y de las observaciones).</summary>
public class AvisarAlProductor(AppDbContext db, ICanalAviso canal) : IObservadorProyecto
{
    public async Task EstadoCambiadoAsync(CambioEstadoProyecto cambio, CancellationToken ct)
    {
        var productor = await db.Users.FindAsync([cambio.Proyecto.ProductorId], ct);
        if (productor?.Email is null) return;
        await new AvisoProyecto(canal, cambio.Proyecto.Nombre, cambio.Proyecto.Estado, cambio.Anterior, cambio.Motivo)
            .EnviarAsync(productor.Email, ct);
    }
}

/// <summary>RF-09: cuando entra un proyecto a evaluación técnica, los analistas reciben el aviso.</summary>
public class AvisarALosAnalistas(UserManager<Usuario> usuarios, ICanalAviso canal) : IObservadorProyecto
{
    public async Task EstadoCambiadoAsync(CambioEstadoProyecto cambio, CancellationToken ct)
    {
        if (cambio.Proyecto.Estado != EstadoProyecto.EnRevision) return;
        foreach (var analista in await usuarios.GetUsersInRoleAsync(Roles.Analista))
            await new AvisoProyectoPorEvaluar(canal, cambio.Proyecto.Nombre).EnviarAsync(analista.Email!, ct);
    }
}
