namespace P11.Api.Models.Projects;

/// <summary>
/// Lo que el analista carga al aprobar un proyecto (RF-10): el nivel de riesgo, el texto de riesgos que
/// lee el inversionista y los supuestos con los que el simulador arma los tres escenarios (RF-12).
/// Los porcentajes son sobre el monto invertido y para todo el plazo del proyecto.
/// </summary>
public record DatosPublicacion(
    NivelRiesgo? NivelRiesgo,
    string? Riesgos,
    decimal? RendimientoPct,
    decimal? CostosPct,
    decimal? MortalidadPct,
    decimal? VariacionPct)
{
    /// <summary>RF-10: sin riesgos y sin los supuestos de los tres escenarios no se publica.</summary>
    public string? Incumplido()
    {
        if (NivelRiesgo is null || string.IsNullOrWhiteSpace(Riesgos))
            return "No se publica un proyecto sin riesgos: indica el nivel y describe los riesgos.";
        if (RendimientoPct is null || CostosPct is null || MortalidadPct is null || VariacionPct is null)
            return "No se publica un proyecto sin los supuestos de los tres escenarios.";
        if (RendimientoPct is < 0 or > 100 || CostosPct is < 0 or > 100 || MortalidadPct is < 0 or > 50)
            return "Los supuestos están fuera de rango: rendimiento y costos entre 0 y 100 %, mortalidad entre 0 y 50 %.";
        if (VariacionPct is < 1 or > 50)
            return "La variación entre escenarios debe estar entre 1 y 50 %.";
        return null;
    }
}
