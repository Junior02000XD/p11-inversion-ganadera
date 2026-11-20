using P11.Api.Dominio;

namespace P11.Api.Servicios.Avisos;

/// <summary>RF-15: la reserva venció o el pago fue rechazado, así que el cupo se liberó.</summary>
public class AvisoCupoLiberado(ICanalAviso canal, string proyecto, decimal montoBs, string motivo) : Aviso(canal)
{
    protected override string Asunto => $"Tu reserva en «{proyecto}» se liberó";

    protected override string Cuerpo =>
        $"La reserva de {FormatoBolivia.Bs(montoBs)} en «{proyecto}» quedó sin efecto: {motivo}\n" +
        "No se te cobró nada. Si todavía hay cupo, puedes volver a reservar desde el catálogo.";
}

/// <summary>RF-16: el pago se confirmó y la participación quedó asignada, con su constancia.</summary>
public class AvisoParticipacion(
    ICanalAviso canal, string proyecto, decimal montoBs, decimal porcentaje, decimal? kg, string constancia) : Aviso(canal)
{
    protected override string Asunto => $"Constancia de participación {constancia}";

    protected override string Cuerpo =>
        $"Confirmamos tu aporte de {FormatoBolivia.Bs(montoBs)} en «{proyecto}».\n" +
        $"Tu participación: {FormatoBolivia.Numero(porcentaje)} % del proyecto" + (kg is null ? "" : $" (≈ {FormatoBolivia.Numero(kg.Value)} kg de ganado)") + ".\n" +
        $"Constancia: {constancia}. La ves en «Consultar portafolio».\n" +
        "Recuerda: la rentabilidad es una estimación, no está garantizada.";
}
