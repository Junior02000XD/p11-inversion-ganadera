namespace P11.Api.Models.Settlements;

public record LineaDetalle(int Nivel, string Nombre, decimal TotalBs);

/// <summary>
/// Composite (RF-23): un concepto de la liquidación es un monto suelto o un grupo de conceptos.
/// Quien calcula o arma el detalle no necesita saber cuál de los dos tiene enfrente.
/// </summary>
public abstract class ConceptoLiquidacion(string nombre)
{
    public string Nombre { get; } = nombre;
    public abstract decimal TotalBs { get; }

    public virtual IEnumerable<LineaDetalle> Detalle(int nivel = 0)
    {
        yield return new LineaDetalle(nivel, Nombre, TotalBs);
    }
}

public class ConceptoSimple : ConceptoLiquidacion
{
    public ConceptoSimple(string nombre, decimal montoBs) : base(nombre)
    {
        if (montoBs < 0) throw new ArgumentOutOfRangeException(nameof(montoBs), "Un concepto no puede tener monto negativo.");
        MontoBs = montoBs;
    }

    public decimal MontoBs { get; }
    public override decimal TotalBs => MontoBs;
}

public class GrupoConceptos(string nombre) : ConceptoLiquidacion(nombre)
{
    private readonly List<ConceptoLiquidacion> _conceptos = [];

    public IReadOnlyList<ConceptoLiquidacion> Conceptos => _conceptos;
    public override decimal TotalBs => _conceptos.Sum(c => c.TotalBs);

    public GrupoConceptos Agregar(ConceptoLiquidacion concepto)
    {
        _conceptos.Add(concepto);
        return this;
    }

    public GrupoConceptos Agregar(string nombre, decimal montoBs) => Agregar(new ConceptoSimple(nombre, montoBs));

    public override IEnumerable<LineaDetalle> Detalle(int nivel = 0)
    {
        yield return new LineaDetalle(nivel, Nombre, TotalBs);
        foreach (var linea in _conceptos.SelectMany(c => c.Detalle(nivel + 1)))
            yield return linea;
    }
}
