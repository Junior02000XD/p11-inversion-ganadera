namespace P11.Api.Servicios.Avisos;

/// <summary>RF-02: aviso de cuenta bloqueada por intentos fallidos. Es urgente: puede ser alguien probando contraseñas.</summary>
public class AvisoBloqueo(ICanalAviso canal, int intentos, DateTimeOffset cuando, DateTimeOffset? hasta) : Aviso(canal)
{
    protected override string Asunto => "Bloqueamos tu cuenta por seguridad";

    protected override string Cuerpo =>
        $"Hubo {intentos} intentos fallidos seguidos de iniciar sesión en tu cuenta " +
        $"({HoraLocal.Formatear(cuando, "dd/MM/yyyy HH:mm")}, hora de Bolivia).\n" +
        $"La cuenta queda bloqueada hasta las {HoraLocal.Formatear(hasta)}. " +
        "Si no fuiste tú, te recomendamos cambiar tu contraseña.";

    protected override bool Urgente => true;
}
