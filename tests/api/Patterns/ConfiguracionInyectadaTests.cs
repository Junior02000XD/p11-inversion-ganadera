using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;

namespace P11.Tests.Patterns;

/// <summary>Configuración inyectada en lugar de Singleton: se valida al arrancar y se sustituye en pruebas.</summary>
public class ConfiguracionInyectadaTests
{
    [Fact]
    public void Una_configuracion_invalida_se_rechaza()
    {
        var sp = new ServiceCollection()
            .AddOptions<ReglasSeguridadOptions>().Configure(o => o.MaxIntentosFallidos = 0).ValidateDataAnnotations().Services
            .BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ReglasSeguridadOptions>>().Value);
    }

    [Fact]
    public void Se_puede_sustituir_sin_estado_global()
    {
        IOptions<ReglasSeguridadOptions> a = Options.Create(new ReglasSeguridadOptions { MaxIntentosFallidos = 3 });
        IOptions<ReglasSeguridadOptions> b = Options.Create(new ReglasSeguridadOptions());
        Assert.Equal(3, a.Value.MaxIntentosFallidos);
        Assert.Equal(5, b.Value.MaxIntentosFallidos);
        Assert.Equal(15, b.Value.MinutosBloqueo);
    }
}
