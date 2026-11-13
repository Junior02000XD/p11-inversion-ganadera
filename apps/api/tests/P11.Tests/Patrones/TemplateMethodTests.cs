using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Servicios.Avisos;
using P11.Tests.Integracion;

namespace P11.Tests.Patrones;

/// <summary>
/// Template Method: ya estaba en el código. <see cref="ProyectoGanadero.Validar"/> fija los pasos comunes y deja
/// <c>ValidarLinea()</c> a cada subclase; <see cref="Aviso.EnviarAsync"/> arma el mensaje y cada aviso pone su texto.
/// </summary>
public class TemplateMethodTests
{
    [Fact]
    public void Validar_aplica_las_reglas_comunes_y_despues_las_de_la_linea()
    {
        var p = new ProyectoLecheria { Nombre = "", Ubicacion = "Warnes", MontoRequeridoBs = 80_000, MontoMinimoBs = 100, PlazoMeses = 12 };

        var campos = p.Validar().Select(e => e.Campo).ToList();

        // Primero el paso común (Nombre) y después el paso que redefine lechería.
        Assert.Equal(["Nombre", "VacasEnOrdeno", "LitrosPorDia", "PrecioLitroBs"], campos);
    }

    [Fact]
    public async Task Cada_aviso_redefine_solo_su_texto_y_el_envio_es_el_mismo()
    {
        var correo = new CorreoEnMemoria();
        var canal = new CanalCorreo(correo);

        await new AvisoProyectoPorEvaluar(canal, "Tambo San Juan").EnviarAsync("ana@test.bo");
        await new AvisoProyecto(canal, "Tambo San Juan", EstadoProyecto.Rechazado, EstadoProyecto.EnRevision, "Sin agua").EnviarAsync("juan@test.bo");

        Assert.Equal(["Proyecto por evaluar: «Tambo San Juan»", "Tu proyecto «Tambo San Juan»: fue rechazado"],
            correo.Enviados.Select(c => c.Asunto));
    }
}
