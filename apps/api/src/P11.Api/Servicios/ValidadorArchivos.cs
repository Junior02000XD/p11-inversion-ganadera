namespace P11.Api.Servicios;

/// <summary>Reconoce el tipo real de un archivo por sus primeros bytes, sin confiar en la extensión.</summary>
public static class ValidadorArchivos
{
    /// <summary>RF-03: una imagen vacía, demasiado chica o que no es JPG/PNG se considera ilegible.</summary>
    public static bool EsImagenLegible(byte[] datos, int tamanoMinimo) =>
        datos.Length >= tamanoMinimo && (EsJpeg(datos) || EsPng(datos));

    public static bool EsPdfOImagen(byte[] datos) => EsPdf(datos) || EsJpeg(datos) || EsPng(datos);

    private static bool EsJpeg(byte[] d) => d is [0xFF, 0xD8, 0xFF, ..];
    private static bool EsPng(byte[] d) => d is [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..];
    private static bool EsPdf(byte[] d) => d is [0x25, 0x50, 0x44, 0x46, ..]; // "%PDF"
}
