namespace P11.Api.Dominio;

/// <summary>Estados de verificación de identidad (RF-04).</summary>
public enum EstadoVerificacion { Pendiente, EnRevision, Verificado, Rechazado }

public enum PropositoOtp { ConfirmarRegistro }

public enum TipoDocumento
{
    // Identidad (RF-03)
    CiFrente, CiDorso, Selfie,
    // Productor y predio (RF-05)
    Runsa, TituloPropiedad, ContratoAlquiler
}

public enum EstadoDocumento { Pendiente, Aprobado, Rechazado }

public enum LineaProductiva { Engorde, Lecheria }

/// <summary>Estados del proyecto (EST-3). El comportamiento de cada uno está en Dominio/Proyectos/Estados.</summary>
public enum EstadoProyecto { Borrador, EnRevision, Rechazado, EnFinanciamiento }
