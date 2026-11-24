namespace P11.Api.Models.Settlements;

/// <summary>
/// Resultado = venta − costos − pérdidas − comisión. Los tres descuentos son conceptos de la liquidación,
/// así que cada uno puede ser un monto o un grupo con su propio detalle.
/// El reparto es una estrategia (<see cref="IReglaReparto"/>); la proporcional al aporte es provisional
/// hasta acordarla con el cliente.
/// </summary>
public class Liquidacion(
    decimal ventaBs, ConceptoLiquidacion costos, ConceptoLiquidacion perdidas, ConceptoLiquidacion comision,
    IReglaReparto? reglaReparto = null)
{
    private readonly IReglaReparto _regla = reglaReparto ?? RepartoProporcional.Instancia;

    public decimal VentaBs { get; } = ventaBs;
    public IReadOnlyList<ConceptoLiquidacion> Descuentos { get; } = [costos, perdidas, comision];

    public decimal ResultadoBs => VentaBs - Descuentos.Sum(d => d.TotalBs);

    public decimal RepartoPara(decimal aporteBs, decimal totalAportadoBs) => _regla.Calcular(ResultadoBs, aporteBs, totalAportadoBs);

    public IEnumerable<LineaDetalle> Detalle() => Descuentos.SelectMany(d => d.Detalle());
}
