using System.Globalization;

namespace P11.Api.Models;

/// <summary>
/// Montos en el formato de Bolivia («Bs 1.000,50») en contratos, mensajes y avisos, sin importar el idioma
/// de la máquina donde corre la API.
/// </summary>
public static class FormatoBolivia
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");

    public static string Bs(decimal monto) => "Bs " + monto.ToString("N2", Cultura);

    /// <summary>Un número con hasta dos decimales: «1,5», «216».</summary>
    public static string Numero(decimal valor) => valor.ToString("#,0.##", Cultura);
}
