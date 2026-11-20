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

    /// <summary>Riesgos y supuestos completos: con esto el analista puede publicar (RF-10).</summary>
    public static readonly DatosPublicacion Publicacion =
        new(NivelRiesgo.Medio, "Mortalidad, enfermedades o caída del precio de venta.", 22, 8, 2, 8);

    public static readonly DateTimeOffset Ahora = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    public static ProyectoGanadero EnRevision()
    {
        var p = Engorde();
        p.EnviarARevision();
        return p;
    }
}
