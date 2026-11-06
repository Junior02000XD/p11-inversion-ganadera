using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;

namespace P11.Tests.Patrones;

/// <summary>Factory Method (RF-07): cada creador produce su línea y aplica sus propias reglas.</summary>
public class FactoryMethodTests
{
    private static readonly Guid Productor = Guid.NewGuid();
    private static readonly SelectorCreadorProyecto Selector = new([new CreadorProyectoEngorde(), new CreadorProyectoLecheria()]);

    private static DatosFicha Engorde(decimal pesoMeta = 420) => new(LineaProductiva.Engorde, "Engorde Chiquitania", "San Ignacio", 100_000, 100, 8,
        CantidadAnimales: 50, PesoInicialKg: 180, PesoMetaKg: pesoMeta, CostosEstimadosBs: 30_000);

    private static DatosFicha Lecheria() => new(LineaProductiva.Lecheria, "Tambo San Juan", "Warnes", 80_000, 100, 12,
        VacasEnOrdeno: 30, LitrosPorDia: 450, PrecioLitroBs: 4.2m);

    [Fact]
    public void Selector_elige_el_creador_de_cada_linea()
    {
        Assert.IsType<CreadorProyectoEngorde>(Selector.Para(LineaProductiva.Engorde));
        Assert.IsType<CreadorProyectoLecheria>(Selector.Para(LineaProductiva.Lecheria));
    }

    [Fact]
    public void Creador_de_engorde_produce_un_borrador_de_engorde()
    {
        var r = Selector.Para(LineaProductiva.Engorde).CrearBorrador(Productor, Engorde());
        Assert.True(r.Exitoso);
        var p = Assert.IsType<ProyectoEngorde>(r.Proyecto);
        Assert.Equal(EstadoProyecto.Borrador, p.Estado);
        Assert.Equal(Productor, p.ProductorId);
        Assert.Equal(420, p.PesoMetaKg);
    }

    [Fact]
    public void Creador_de_lecheria_ignora_los_campos_de_engorde()
    {
        var datos = Lecheria() with { CantidadAnimales = 99, PesoInicialKg = 1 };
        var p = Assert.IsType<ProyectoLecheria>(Selector.Para(LineaProductiva.Lecheria).CrearBorrador(Productor, datos).Proyecto);
        Assert.Equal(30, p.VacasEnOrdeno);
    }

    [Fact]
    public void Lecheria_sin_sus_campos_no_se_crea_y_marca_cada_campo()
    {
        var datos = new DatosFicha(LineaProductiva.Lecheria, "Tambo", "Warnes", 80_000, 100, 12);
        var r = Selector.Para(LineaProductiva.Lecheria).CrearBorrador(Productor, datos);
        Assert.False(r.Exitoso);
        Assert.Null(r.Proyecto);
        Assert.Equal(["VacasEnOrdeno", "LitrosPorDia", "PrecioLitroBs"], r.Errores.Select(e => e.Campo));
    }

    [Fact]
    public void Engorde_exige_peso_meta_mayor_al_inicial()
    {
        var r = Selector.Para(LineaProductiva.Engorde).CrearBorrador(Productor, Engorde(pesoMeta: 150));
        Assert.Contains(r.Errores, e => e.Campo == nameof(ProyectoEngorde.PesoMetaKg));
    }

    [Fact]
    public void Reglas_comunes_se_aplican_a_todas_las_lineas()
    {
        var datos = Lecheria() with { Nombre = " ", MontoMinimoBs = 90_000 };
        var r = Selector.Para(LineaProductiva.Lecheria).CrearBorrador(Productor, datos);
        Assert.Contains(r.Errores, e => e.Campo == "Nombre");
        Assert.Contains(r.Errores, e => e.Campo == "MontoMinimoBs");
    }

    [Fact]
    public void Un_creador_no_acepta_datos_de_otra_linea()
    {
        Assert.Throws<ArgumentException>(() => new CreadorProyectoEngorde().CrearBorrador(Productor, Lecheria()));
    }
}
