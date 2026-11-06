using P11.Api.Dominio.Expediente;

namespace P11.Tests.Patrones;

/// <summary>Builder (RF-22): orden de la cronología, pertenencia, partes obligatorias y estado no compartido.</summary>
public class BuilderTests
{
    private static readonly Guid Proyecto = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static ResumenFicha Ficha => new(Proyecto, "Engorde Chiquitania", "Engorde", 100_000, "EnFinanciamiento");

    [Fact]
    public void Ordena_la_cronologia_por_fecha()
    {
        var vista = new ConstructorExpediente()
            .ConFicha(Ficha)
            .AgregarEvento(new(Proyecto, TipoEvento.Avance, T0.AddDays(30), "Pesaje mes 1"))
            .AgregarEvento(new(Proyecto, TipoEvento.Contrato, T0, "Contrato firmado"))
            .AgregarEvento(new(Proyecto, TipoEvento.Compra, T0.AddDays(5), "Compra de 50 cabezas"))
            .Construir();

        Assert.Equal([TipoEvento.Contrato, TipoEvento.Compra, TipoEvento.Avance], vista.Cronologia.Select(e => e.Tipo));
        Assert.False(vista.EsVersionDeCierre);
    }

    [Fact]
    public void Sin_ficha_informa_la_inconsistencia()
    {
        Assert.Throws<ExpedienteIncompletoException>(() => new ConstructorExpediente().Construir());
    }

    [Fact]
    public void Rechaza_eventos_de_otro_proyecto()
    {
        var b = new ConstructorExpediente().ConFicha(Ficha)
            .AgregarEvento(new(Guid.NewGuid(), TipoEvento.Avance, T0, "Ajeno"));
        Assert.Throws<ExpedienteIncompletoException>(() => b.Construir());
    }

    [Fact]
    public void Version_de_cierre_exige_venta_y_liquidacion()
    {
        var b = new ConstructorExpediente().ConFicha(Ficha).ComoVersionDeCierre()
            .AgregarEvento(new(Proyecto, TipoEvento.Venta, T0.AddDays(240), "Venta"));
        var ex = Assert.Throws<ExpedienteIncompletoException>(() => b.Construir());
        Assert.Contains("Liquidacion", ex.Message);
    }

    [Fact]
    public void Expediente_en_curso_puede_omitir_la_venta()
    {
        var vista = new ConstructorExpediente().ConFicha(Ficha)
            .AgregarDocumento(new(Guid.NewGuid(), Proyecto, "Contrato", T0)).Construir();
        Assert.Single(vista.Documentos);
        Assert.Empty(vista.Cronologia);
    }

    [Fact]
    public void Dos_construcciones_no_comparten_estado_y_un_constructor_no_se_reutiliza()
    {
        var a = new ConstructorExpediente().ConFicha(Ficha).AgregarEvento(new(Proyecto, TipoEvento.Contrato, T0, "A"));
        var b = new ConstructorExpediente().ConFicha(Ficha);
        Assert.Single(a.Construir().Cronologia);
        Assert.Empty(b.Construir().Cronologia);
        Assert.Throws<InvalidOperationException>(() => a.AgregarEvento(new(Proyecto, TipoEvento.Avance, T0, "tarde")));
    }
}
