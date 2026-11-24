using P11.Api.Models.Settlements;

namespace P11.Tests.Patterns;

/// <summary>
/// Iterator: lo da el lenguaje (IEnumerable y yield). <see cref="GrupoConceptos.Detalle"/> recorre el árbol del
/// Composite sin exponer cómo guarda sus hijos, y lo hace a demanda: solo avanza cuando se le pide el siguiente.
/// </summary>
public class IteratorTests
{
    [Fact]
    public void El_recorrido_es_perezoso()
    {
        var costos = new GrupoConceptos("Costos").Agregar("Compra", 45_000).Agregar(new GrupoConceptos("Sanidad").Agregar("Vacunas", 4_000));

        using var it = costos.Detalle().GetEnumerator();
        Assert.True(it.MoveNext());
        Assert.Equal(("Costos", 0), (it.Current.Nombre, it.Current.Nivel));

        // El recorrido completo es en preorden: el grupo y después sus hijos, también los anidados.
        Assert.Equal(["Costos", "Compra", "Sanidad", "Vacunas"], costos.Detalle().Select(l => l.Nombre));
    }

    [Fact]
    public void Dos_recorridos_del_mismo_arbol_no_se_estorban()
    {
        var g = new GrupoConceptos("Pérdidas").Agregar("Muerte de 1 animal", 3_000);
        using var a = g.Detalle().GetEnumerator();
        using var b = g.Detalle().GetEnumerator();
        a.MoveNext(); a.MoveNext();
        b.MoveNext();
        Assert.Equal("Muerte de 1 animal", a.Current.Nombre);
        Assert.Equal("Pérdidas", b.Current.Nombre);
    }
}
