using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;

namespace P11.Tests.Patrones;

/// <summary>Fichas válidas para las pruebas de los patrones de comportamiento.</summary>
internal static class Fichas
{
    public static ProyectoGanadero Engorde(Guid? productor = null, string nombre = "Engorde Chiquitania") =>
        new CreadorProyectoEngorde().CrearBorrador(productor ?? Guid.NewGuid(),
            new DatosFicha(LineaProductiva.Engorde, nombre, "San Ignacio", 100_000, 100, 8,
                CantidadAnimales: 50, PesoInicialKg: 180, PesoMetaKg: 420, CostosEstimadosBs: 30_000)).Proyecto!;

    public static ProyectoGanadero EnRevision()
    {
        var p = Engorde();
        p.EnviarARevision();
        return p;
    }
}
