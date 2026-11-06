using System.ComponentModel.DataAnnotations;

namespace P11.Api.Configuracion;

// Configuración inyectada en lugar de Singleton (análisis de patrones, §8.5): cada sección de
// appsettings se valida al arrancar y llega a los servicios como dependencia explícita (IOptions<T>).
// No hay acceso global ni estado compartido mutable, y en las pruebas se reemplaza sin trucos.

/// <summary>Reglas de RF-02: bloqueo al 5.º intento fallido durante 15 minutos.</summary>
public class ReglasSeguridadOptions
{
    public const string Seccion = "ReglasSeguridad";
    [Range(1, 20)] public int MaxIntentosFallidos { get; set; } = 5;
    [Range(1, 1440)] public int MinutosBloqueo { get; set; } = 15;
    [Range(8, 128)] public int LongitudMinimaContrasena { get; set; } = 8;
}

/// <summary>Código OTP por correo (RNF-01).</summary>
public class OtpOptions
{
    public const string Seccion = "Otp";
    [Range(4, 8)] public int Digitos { get; set; } = 6;
    [Range(1, 60)] public int MinutosVigencia { get; set; } = 10;
    [Range(1, 10)] public int MaxIntentos { get; set; } = 3;
}

/// <summary>Almacenamiento cifrado de archivos (RNF-02).</summary>
public class ArchivosOptions
{
    public const string Seccion = "Archivos";
    [Required] public string Carpeta { get; set; } = "App_Data/archivos";
    /// <summary>Clave AES-256 en base64 (32 bytes). Nunca en el repositorio.</summary>
    [Required] public string ClaveBase64 { get; set; } = "";
    [Range(1, 50_000_000)] public int TamanoMaximoBytes { get; set; } = 5_000_000;
    /// <summary>Por debajo de este tamaño la imagen se considera ilegible (RF-03).</summary>
    [Range(1, 1_000_000)] public int TamanoMinimoImagenBytes { get; set; } = 1024;

    public static bool EsClaveAes256(string base64)
    {
        try { return Convert.FromBase64String(base64).Length == 32; }
        catch (FormatException) { return false; }
    }
}

public class JwtOptions
{
    public const string Seccion = "Jwt";
    [Required] public string Emisor { get; set; } = "p11-api";
    [Required] public string Audiencia { get; set; } = "p11-clientes";
    [Required, MinLength(32)] public string ClaveFirma { get; set; } = "";
    [Range(5, 1440)] public int MinutosVigencia { get; set; } = 120;
}

public class CorreoOptions
{
    public const string Seccion = "Correo";
    [Required] public string Host { get; set; } = "localhost";
    [Range(1, 65535)] public int Puerto { get; set; } = 1025;
    public bool UsarSsl { get; set; }
    public string? Usuario { get; set; }
    public string? Contrasena { get; set; }
    [Required, EmailAddress] public string Remitente { get; set; } = "no-responder@p11.local";
    public string NombreRemitente { get; set; } = "Plataforma de Inversión Ganadera";
}

/// <summary>Proveedor de verificación de identidad simulado (RF-03).</summary>
public class VerificacionOptions
{
    public const string Seccion = "Verificacion";
    /// <summary>Coincidencia rostro-documento mínima para verificar sin revisión manual.</summary>
    [Range(0.0, 1.0)] public double UmbralCoincidencia { get; set; } = 0.80;
    /// <summary>Puntaje que devuelve el simulador cuando no se fuerza la revisión.</summary>
    [Range(0.0, 1.0)] public double PuntajeSimulado { get; set; } = 0.92;
}
