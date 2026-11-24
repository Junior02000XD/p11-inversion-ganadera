namespace P11.Api.Models;

/// <summary>Estados de verificación de identidad (RF-04).</summary>
public enum EstadoVerificacion { Pendiente, EnRevision, Verificado, Rechazado }

/// <summary>Para qué sirve el código OTP: confirmar la cuenta (RF-01) o firmar el contrato de inversión (RF-14).</summary>
public enum PropositoOtp { ConfirmarRegistro, FirmarContrato }

public enum TipoDocumento
{
    // Identidad (RF-03)
    CiFrente, CiDorso, Selfie,
    // Productor y predio (RF-05)
    Runsa, TituloPropiedad, ContratoAlquiler
}

public enum EstadoDocumento { Pendiente, Aprobado, Rechazado }

public enum LineaProductiva { Engorde, Lecheria }

/// <summary>Estados del proyecto (EST-3). El comportamiento de cada uno está en Models/Projects/States.</summary>
public enum EstadoProyecto { Borrador, EnRevision, Rechazado, EnFinanciamiento }

/// <summary>Nivel de riesgo que asigna el analista al publicar (RF-10); el catálogo filtra por él (RF-11).</summary>
public enum NivelRiesgo { Bajo, Medio, Alto }
