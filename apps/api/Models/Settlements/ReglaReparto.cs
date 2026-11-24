namespace P11.Api.Models.Settlements;

/// <summary>
/// Strategy (GoF): cómo se reparte el resultado de la liquidación entre los inversionistas (RF-23).
/// El ERS deja abierto el reparto («deben salir de un modelo financiero validado»): cuando el cliente
/// lo defina, la regla nueva es otra clase y <see cref="Liquidacion"/> no cambia.
/// </summary>
public interface IReglaReparto
{
    decimal Calcular(decimal resultadoBs, decimal aporteBs, decimal totalAportadoBs);
}

/// <summary>Regla provisional: a cada uno le toca la parte proporcional a lo que aportó.</summary>
public sealed class RepartoProporcional : IReglaReparto
{
    public static readonly RepartoProporcional Instancia = new();

    public decimal Calcular(decimal resultadoBs, decimal aporteBs, decimal totalAportadoBs)
    {
        if (totalAportadoBs <= 0) throw new ArgumentOutOfRangeException(nameof(totalAportadoBs));
        return Math.Round(resultadoBs * aporteBs / totalAportadoBs, 2);
    }
}
