namespace P11.Api.Dominio.Liquidaciones;

/// <summary>
/// Resultado = venta − costos − pérdidas − comisión. Los tres descuentos son conceptos de la liquidación,
/// así que cada uno puede ser un monto o un grupo con su propio detalle.
/// El reparto proporcional al aporte es provisional hasta acordarlo con el cliente.
/// </summary>
public class Liquidacion(decimal ventaBs, ConceptoLiquidacion costos, ConceptoLiquidacion perdidas, ConceptoLiquidacion comision)
{
    public decimal VentaBs { get; } = ventaBs;
    public IReadOnlyList<ConceptoLiquidacion> Descuentos { get; } = [costos, perdidas, comision];

    public decimal ResultadoBs => VentaBs - Descuentos.Sum(d => d.TotalBs);

    public decimal RepartoPara(decimal aporteBs, decimal totalAportadoBs)
    {
        if (totalAportadoBs <= 0) throw new ArgumentOutOfRangeException(nameof(totalAportadoBs));
        return Math.Round(ResultadoBs * aporteBs / totalAportadoBs, 2);
    }

    public IEnumerable<LineaDetalle> Detalle() => Descuentos.SelectMany(d => d.Detalle());
}
