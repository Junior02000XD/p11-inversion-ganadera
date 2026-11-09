using P11.Api.Dominio.Liquidaciones;

namespace P11.Tests.Patrones;

/// <summary>Composite (RF-23): montos sueltos y grupos de conceptos se suman y se recorren igual.</summary>
public class CompositeTests
{
    private static Liquidacion Ejemplo()
    {
        var sanidad = new GrupoConceptos("Sanidad")
            .Agregar("Vacunas", 4_000)
            .Agregar("Veterinario", 2_500);
        var costos = new GrupoConceptos("Costos")
            .Agregar("Compra de terneros", 45_000)
            .Agregar("Traslado", 3_500)
            .Agregar(sanidad)
            .Agregar("Alimentación", 7_000);
        var perdidas = new GrupoConceptos("Pérdidas").Agregar("Muerte de 1 animal", 3_000);
        var comision = new ConceptoSimple("Comisión de la plataforma", 2_500);

        return new Liquidacion(ventaBs: 130_000, costos, perdidas, comision);
    }

    [Fact]
    public void Un_grupo_suma_tambien_los_grupos_que_contiene()
    {
        var costos = Ejemplo().Descuentos[0];
        Assert.Equal(62_000, costos.TotalBs);
    }

    [Fact]
    public void El_resultado_es_venta_menos_costos_perdidas_y_comision()
    {
        Assert.Equal(130_000 - 62_000 - 3_000 - 2_500, Ejemplo().ResultadoBs);
    }

    [Fact]
    public void El_detalle_respeta_la_jerarquia()
    {
        var detalle = Ejemplo().Detalle().ToList();

        Assert.Contains(new LineaDetalle(0, "Costos", 62_000), detalle);
        Assert.Contains(new LineaDetalle(1, "Sanidad", 6_500), detalle);
        Assert.Contains(new LineaDetalle(2, "Vacunas", 4_000), detalle);
        Assert.Contains(new LineaDetalle(0, "Comisión de la plataforma", 2_500), detalle);
    }

    [Fact]
    public void Reparte_el_resultado_segun_el_aporte()
    {
        // Aportó 20 000 de 100 000: le toca el 20 % de 62 500.
        Assert.Equal(12_500, Ejemplo().RepartoPara(aporteBs: 20_000, totalAportadoBs: 100_000));
    }

    [Fact]
    public void Rechaza_montos_negativos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConceptoSimple("Ajuste", -1));
    }
}
