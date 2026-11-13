using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Dominio.Proyectos.Estados;
using P11.Api.Servicios.Proyectos;

namespace P11.Tests.Patrones;

/// <summary>Command (RF-09, RNF-03): cada acción es un objeto; el invocador la ejecuta y deja la bitácora.</summary>
public class CommandTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 13, 14, 0, 0, TimeSpan.Zero);

    private static async Task<(AppDbContext db, GestorCicloProyecto gestor, ProyectoGanadero proyecto)> Preparar()
    {
        var db = ChainOfResponsibilityTests.BaseEnMemoria();
        var proyecto = Fichas.Engorde();
        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync();
        var gestor = new GestorCicloProyecto(db, [], new FakeTimeProvider(Ahora), NullLogger<GestorCicloProyecto>.Instance);
        return (db, gestor, proyecto);
    }

    [Fact]
    public async Task Cada_comando_ejecutado_queda_en_la_bitacora_con_responsable_motivo_y_fecha()
    {
        var (db, gestor, p) = await Preparar();

        await gestor.EjecutarAsync(new EnviarARevision(p.Id, "productor@test.bo"));
        await gestor.EjecutarAsync(new ObservarProyecto(p.Id, "analista@test.bo", "Falta el plan sanitario."));

        var bitacora = db.HistorialProyectos.OrderBy(h => h.Accion == "Observar").ToList();
        Assert.Equal(2, bitacora.Count);
        Assert.Equal(("Enviar a revisión", EstadoProyecto.Borrador, EstadoProyecto.EnRevision, "productor@test.bo"),
            (bitacora[0].Accion, bitacora[0].EstadoAnterior, bitacora[0].EstadoNuevo, bitacora[0].Responsable));
        Assert.Equal(("Observar", EstadoProyecto.EnRevision, EstadoProyecto.Borrador, "Falta el plan sanitario.", Ahora),
            (bitacora[1].Accion, bitacora[1].EstadoAnterior, bitacora[1].EstadoNuevo, bitacora[1].Motivo, bitacora[1].Fecha));
    }

    [Fact]
    public async Task Un_comando_que_el_estado_no_admite_no_cambia_nada_ni_deja_bitacora()
    {
        var (db, gestor, p) = await Preparar();

        await Assert.ThrowsAsync<TransicionInvalidaException>(() =>
            gestor.EjecutarAsync(new RechazarProyecto(p.Id, "analista@test.bo", "No corresponde")));

        Assert.Equal(EstadoProyecto.Borrador, p.Estado);
        Assert.Empty(db.HistorialProyectos);
    }

    [Fact]
    public async Task Aprobar_sin_riesgos_ni_tres_escenarios_no_publica()
    {
        var (_, gestor, p) = await Preparar();
        await gestor.EjecutarAsync(new EnviarARevision(p.Id, "productor@test.bo"));

        var ex = await Assert.ThrowsAsync<TransicionInvalidaException>(() =>
            gestor.EjecutarAsync(new AprobarYPublicarProyecto(p.Id, "analista@test.bo", "ok", RiesgosCargados: true, TresEscenarios: false)));
        Assert.Contains("tres escenarios", ex.Message);
        Assert.Equal(EstadoProyecto.EnRevision, p.Estado);

        await gestor.EjecutarAsync(new AprobarYPublicarProyecto(p.Id, "analista@test.bo", "ok", true, true));
        Assert.Equal(EstadoProyecto.EnFinanciamiento, p.Estado);
    }

    [Fact]
    public async Task Un_proyecto_inexistente_devuelve_null()
    {
        var (_, gestor, _) = await Preparar();
        Assert.Null(await gestor.EjecutarAsync(new EnviarARevision(Guid.NewGuid(), "x")));
    }
}
