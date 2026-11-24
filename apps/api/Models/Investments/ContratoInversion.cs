using P11.Api.Models.Projects;

namespace P11.Api.Models.Investments;

/// <summary>
/// Texto del contrato que el inversionista acepta con un código OTP (RF-14). La versión queda guardada en
/// la inversión: si el texto cambia, se sube la versión y se sabe qué aceptó cada uno.
/// </summary>
public static class ContratoInversion
{
    public const string Version = "1.0 (2026-11)";

    public static string Texto(ProyectoGanadero proyecto, decimal montoBs) =>
        $"""
        CONTRATO DE PARTICIPACIÓN EN PROYECTO GANADERO — versión {Version}

        1. El inversionista aporta {FormatoBolivia.Bs(montoBs)} al proyecto «{proyecto.Nombre}» ({proyecto.Ubicacion}), con un plazo estimado de {proyecto.PlazoMeses} meses.
        2. A cambio recibe una participación proporcional a su aporte sobre la meta del proyecto ({FormatoBolivia.Bs(proyecto.MontoRequeridoBs)}).
        3. Al cierre, el resultado de la venta, menos los costos, las pérdidas y la comisión de la plataforma, se reparte según esa participación.
        4. La rentabilidad NO está garantizada. Los escenarios del simulador son estimaciones y el resultado puede ser menor al aporte.
        5. Riesgos declarados por el equipo técnico: {proyecto.Riesgos}
        6. Si la ronda no alcanza el financiamiento mínimo, el aporte se devuelve completo.
        """;
}
