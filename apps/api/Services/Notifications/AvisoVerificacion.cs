using P11.Api.Models;
using P11.Api.Services.Verification;

namespace P11.Api.Services.Notifications;

/// <summary>RF-04: cada cambio de estado de la verificación se le avisa al usuario.</summary>
public class AvisoVerificacion(ICanalAviso canal, EstadoVerificacion estado, string motivo) : Aviso(canal)
{
    protected override string Asunto => $"Verificación de identidad: {VerificacionService.Texto(estado)}";

    protected override string Cuerpo => estado switch
    {
        EstadoVerificacion.Verificado => "Verificamos tu identidad. Ya puedes operar en la plataforma.",
        EstadoVerificacion.EnRevision => "Recibimos tus documentos. Un analista va a revisar tu identidad.",
        EstadoVerificacion.Rechazado => "No pudimos verificar tu identidad. Puedes volver a intentarlo con fotos nuevas.",
        _ => "El estado de tu verificación cambió.",
    } + $"\nMotivo: {motivo}";
}
