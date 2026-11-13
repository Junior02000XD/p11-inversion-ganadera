using P11.Api.Dominio.Liquidaciones;

namespace P11.Tests.Patrones;

/// <summary>Strategy (RF-23): la regla de reparto se cambia sin tocar la liquidación.</summary>
public class StrategyTests
{
    private static Liquidacion Con(IReglaReparto? regla = null) =>
        new(ventaBs: 130_000, new ConceptoSimple("Costos", 62_000), new ConceptoSimple("Pérdidas", 3_000),
            new ConceptoSimple("Comisión", 2_500), regla);

    // Solo para la prueba: una regla distinta que reparte en partes iguales entre 4 inversionistas.
    // No es una regla del negocio; la definitiva sale del modelo financiero con el cliente.
    private class PartesIguales(int inversionistas) : IReglaReparto
    {
        public decimal Calcular(decimal resultadoBs, decimal aporteBs, decimal totalAportadoBs) => resultadoBs / inversionistas;
    }

    [Fact]
    public void Sin_indicar_regla_se_usa_la_proporcional_al_aporte()
    {
        // Resultado 62 500; quien aportó 20 000 de 100 000 recibe el 20 %.
        Assert.Equal(12_500, Con().RepartoPara(aporteBs: 20_000, totalAportadoBs: 100_000));
    }

    [Fact]
    public void Otra_regla_cambia_el_reparto_sin_cambiar_el_calculo_del_resultado()
    {
        var liquidacion = Con(new PartesIguales(4));
        Assert.Equal(62_500, liquidacion.ResultadoBs);
        Assert.Equal(15_625, liquidacion.RepartoPara(20_000, 100_000));
    }

    [Fact]
    public void La_regla_proporcional_rechaza_un_total_aportado_de_cero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RepartoProporcional.Instancia.Calcular(1_000, 10, 0));
    }
}
