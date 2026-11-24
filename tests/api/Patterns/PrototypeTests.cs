using P11.Api.Models.Simulation;

namespace P11.Tests.Patterns;

/// <summary>Prototype (RF-12): las variantes son copias independientes de la base.</summary>
public class PrototypeTests
{
    private static ConfiguracionEscenario Base()
    {
        var c = new ConfiguracionEscenario { ProyectoId = Guid.NewGuid(), VersionSupuestos = 3, MontoInversionBs = 10_000, PlazoMeses = 8 };
        c.Supuestos["rendimiento"] = 0.20m;
        c.Supuestos["costos"] = 0.05m;
        c.Riesgos.Add("Mortalidad");
        return c;
    }

    [Fact]
    public void Clonar_da_identidad_nueva_y_conserva_los_valores()
    {
        var b = Base();
        var copia = b.Clonar();
        Assert.NotEqual(b.IdSimulacion, copia.IdSimulacion);
        Assert.Equal(b.ProyectoId, copia.ProyectoId);
        Assert.Equal(b.VersionSupuestos, copia.VersionSupuestos);
        Assert.Equal(b.Supuestos, copia.Supuestos);
    }

    [Fact]
    public void Modificar_la_copia_no_altera_la_base_ni_otras_copias()
    {
        var b = Base();
        var escenarios = PreparadorEscenarios.Preparar(b);
        var optimista = escenarios.Single(e => e.Tipo == TipoEscenario.Optimista);
        var pesimista = escenarios.Single(e => e.Tipo == TipoEscenario.Pesimista);

        optimista.Supuestos["costos"] = 0.01m;
        optimista.Riesgos.Add("Sequía");

        Assert.Equal(0.05m, b.Supuestos["costos"]);
        Assert.Equal(0.05m, pesimista.Supuestos["costos"]);
        Assert.Single(b.Riesgos);
        Assert.Single(pesimista.Riesgos);
        Assert.NotSame(b.Supuestos, optimista.Supuestos);
    }

    [Fact]
    public void Prepara_tres_variantes_ordenadas_y_con_advertencia()
    {
        var resultados = PreparadorEscenarios.Preparar(Base()).Select(e => e.Calcular()).ToList();
        Assert.Equal([TipoEscenario.Pesimista, TipoEscenario.Esperado, TipoEscenario.Optimista], resultados.Select(r => r.Tipo));
        Assert.True(resultados[0].ValorEstimadoBs < resultados[1].ValorEstimadoBs);
        Assert.True(resultados[1].ValorEstimadoBs < resultados[2].ValorEstimadoBs);
        Assert.All(resultados, r => Assert.Contains("no rentabilidad garantizada", r.Advertencia));
    }

    [Fact]
    public void La_copia_no_hereda_el_resultado_anterior()
    {
        var b = Base();
        b.Calcular();
        Assert.NotNull(b.Resultado);
        Assert.Null(b.Clonar().Resultado);
    }
}
