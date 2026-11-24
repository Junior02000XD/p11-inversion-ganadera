using P11.Api.Models;
using P11.Api.Models.Projects;
using P11.Api.Models.Projects.States;

namespace P11.Tests.Patterns;

/// <summary>State (EST-3): cada estado del proyecto decide qué operaciones admite y a dónde pasa.</summary>
public class StateTests
{
    [Fact]
    public void Un_borrador_se_envia_a_revision_y_ya_no_se_puede_editar()
    {
        var p = Fichas.Engorde();
        Assert.IsType<EnBorrador>(p.EstadoActual);
        Assert.True(p.EstadoActual.PuedeEditarse);

        p.EnviarARevision();

        Assert.Equal(EstadoProyecto.EnRevision, p.Estado);
        Assert.IsType<EnRevisionTecnica>(p.EstadoActual);
        Assert.False(p.EstadoActual.PuedeEditarse);
    }

    [Fact]
    public void Observar_devuelve_el_proyecto_al_productor_con_las_observaciones()
    {
        var p = Fichas.EnRevision();

        p.Observar("Falta detallar la alimentación.");

        Assert.Equal(EstadoProyecto.Borrador, p.Estado);
        Assert.Equal("Falta detallar la alimentación.", p.Observaciones);
        p.EnviarARevision(); // corregido, vuelve a entrar
        Assert.Equal(EstadoProyecto.EnRevision, p.Estado);
    }

    [Fact]
    public void Aprobar_publica_el_proyecto()
    {
        var p = Fichas.EnRevision();
        p.AprobarYPublicar(Fichas.Publicacion, Fichas.Ahora);
        Assert.Equal(EstadoProyecto.EnFinanciamiento, p.Estado);
        Assert.Equal(Fichas.Ahora.AddDays(DatosPublicacion.DiasRonda), p.CierreRondaEn);
    }

    [Fact]
    public void Un_borrador_no_se_puede_aprobar_ni_observar()
    {
        var p = Fichas.Engorde();

        var ex = Assert.Throws<TransicionInvalidaException>(() => p.AprobarYPublicar(Fichas.Publicacion, Fichas.Ahora));
        Assert.Equal("Un proyecto en borrador no se puede aprobar ni publicar.", ex.Message);
        Assert.Throws<TransicionInvalidaException>(() => p.Observar("x"));
        Assert.Equal(EstadoProyecto.Borrador, p.Estado);
    }

    [Fact]
    public void Un_proyecto_rechazado_es_final()
    {
        var p = Fichas.EnRevision();
        p.Rechazar("El predio no tiene agua todo el año.");

        Assert.Equal(EstadoProyecto.Rechazado, p.Estado);
        Assert.Equal("El predio no tiene agua todo el año.", p.Observaciones);
        Assert.Throws<TransicionInvalidaException>(p.EnviarARevision);
        Assert.Throws<TransicionInvalidaException>(() => p.AprobarYPublicar(Fichas.Publicacion, Fichas.Ahora));
    }

    [Fact]
    public void Un_proyecto_en_revision_no_se_vuelve_a_enviar()
    {
        var p = Fichas.EnRevision();
        var ex = Assert.Throws<TransicionInvalidaException>(p.EnviarARevision);
        Assert.Equal("Un proyecto en revisión técnica no se puede enviar a evaluación.", ex.Message);
    }

    [Fact]
    public void Cada_valor_del_enum_tiene_su_clase_de_estado()
    {
        foreach (var valor in Enum.GetValues<EstadoProyecto>())
            Assert.Equal(valor, EstadoFicha.De(valor).Valor);
    }
}
