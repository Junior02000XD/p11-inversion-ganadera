using Microsoft.Extensions.Logging.Abstractions;
using P11.Api.Dominio;
using P11.Api.Servicios.Avisos;
using P11.Api.Servicios.Proyectos;
using P11.Tests.Integracion;

namespace P11.Tests.Patrones;

/// <summary>Observer (RF-09): los interesados se enteran de cada cambio de estado sin que el gestor los conozca.</summary>
public class ObserverTests
{
    private class Anotador : IObservadorProyecto
    {
        public List<(EstadoProyecto Anterior, EstadoProyecto Nuevo)> Cambios { get; } = [];
        public Task EstadoCambiadoAsync(CambioEstadoProyecto c, CancellationToken ct)
        {
            Cambios.Add((c.Anterior, c.Proyecto.Estado));
            return Task.CompletedTask;
        }
    }

    private class QueFalla : IObservadorProyecto
    {
        public Task EstadoCambiadoAsync(CambioEstadoProyecto c, CancellationToken ct) => throw new InvalidOperationException("SMTP caído");
    }

    [Fact]
    public async Task Todos_los_observadores_reciben_el_cambio_aunque_uno_falle()
    {
        using var db = ChainOfResponsibilityTests.BaseEnMemoria();
        var p = Fichas.Engorde();
        db.Proyectos.Add(p);
        await db.SaveChangesAsync();
        var primero = new Anotador();
        var ultimo = new Anotador();
        var gestor = new GestorCicloProyecto(db, [primero, new QueFalla(), ultimo], TimeProvider.System, NullLogger<GestorCicloProyecto>.Instance);

        await gestor.EjecutarAsync(new EnviarARevision(p.Id, "productor@test.bo"));

        Assert.Equal([(EstadoProyecto.Borrador, EstadoProyecto.EnRevision)], primero.Cambios);
        Assert.Equal(primero.Cambios, ultimo.Cambios);
        Assert.Equal(EstadoProyecto.EnRevision, p.Estado); // la decisión no se revierte
    }

    [Fact]
    public async Task El_productor_recibe_las_observaciones_por_correo()
    {
        using var db = ChainOfResponsibilityTests.BaseEnMemoria();
        var productor = new Usuario { UserName = "juan@test.bo", Email = "juan@test.bo", Ci = "1", NombreCompleto = "Juan Pérez" };
        db.Users.Add(productor);
        var p = Fichas.Engorde(productor.Id, "Engorde Montero");
        db.Proyectos.Add(p);
        await db.SaveChangesAsync();
        var correo = new CorreoEnMemoria();
        var gestor = new GestorCicloProyecto(db, [new AvisarAlProductor(db, new CanalCorreo(correo))],
            TimeProvider.System, NullLogger<GestorCicloProyecto>.Instance);

        await gestor.EjecutarAsync(new EnviarARevision(p.Id, "juan@test.bo"));
        await gestor.EjecutarAsync(new ObservarProyecto(p.Id, "analista@test.bo", "Falta detallar la alimentación."));

        var avisos = correo.Para("juan@test.bo").ToList();
        Assert.Equal(2, avisos.Count);
        Assert.Equal("Tu proyecto «Engorde Montero»: en evaluación técnica", avisos[0].Asunto);
        Assert.Equal("Tu proyecto «Engorde Montero»: tiene observaciones", avisos[1].Asunto);
        Assert.Contains("Falta detallar la alimentación.", avisos[1].Cuerpo);
    }
}
