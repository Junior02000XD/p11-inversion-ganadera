namespace P11.Api.Servicios;

/// <summary>Formatea horas para los mensajes al usuario en hora de Bolivia (UTC−4, sin horario de verano).</summary>
public static class HoraLocal
{
    private static readonly TimeZoneInfo Bolivia = Buscar();

    public static string Formatear(DateTimeOffset? instante, string formato = "HH:mm") =>
        instante is null ? "" : TimeZoneInfo.ConvertTime(instante.Value, Bolivia).ToString(formato);

    private static TimeZoneInfo Buscar()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("BOT", TimeSpan.FromHours(-4), "Bolivia", "Bolivia"); }
    }
}
