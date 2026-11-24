using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration.Memory;

namespace P11.Api.Configuration;

/// <summary>
/// En desarrollo, genera la clave de cifrado de archivos y la de firma de tokens la primera vez que
/// arranca la API y las guarda en App_Data/claves-desarrollo.json (ignorado por git). Así el proyecto
/// funciona recién clonado. Se cargan con la menor prioridad: user-secrets y variables de entorno
/// las reemplazan. Fuera de desarrollo no se genera nada y la API exige claves explícitas.
/// </summary>
public static class ClavesDesarrollo
{
    private const string ClaveArchivos = "Archivos:ClaveBase64";
    private const string ClaveJwt = "Jwt:ClaveFirma";

    public static void Agregar(ConfigurationManager config, string contentRoot)
    {
        var ruta = Path.Combine(contentRoot, "App_Data", "claves-desarrollo.json");
        var claves = File.Exists(ruta) ? Leer(ruta) : new Dictionary<string, string?>();

        var cambio = false;
        if (string.IsNullOrEmpty(claves.GetValueOrDefault(ClaveArchivos)))
        {
            claves[ClaveArchivos] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            cambio = true;
        }
        if (string.IsNullOrEmpty(claves.GetValueOrDefault(ClaveJwt)))
        {
            claves[ClaveJwt] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            cambio = true;
        }
        if (cambio) Guardar(ruta, claves);

        // Índice 0: la fuente con menor prioridad.
        config.Sources.Insert(0, new MemoryConfigurationSource { InitialData = claves });
    }

    private static Dictionary<string, string?> Leer(string ruta)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
        var r = doc.RootElement;
        return new Dictionary<string, string?>
        {
            [ClaveArchivos] = r.TryGetProperty("Archivos", out var a) && a.TryGetProperty("ClaveBase64", out var k) ? k.GetString() : null,
            [ClaveJwt] = r.TryGetProperty("Jwt", out var j) && j.TryGetProperty("ClaveFirma", out var f) ? f.GetString() : null,
        };
    }

    private static void Guardar(string ruta, Dictionary<string, string?> claves)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        var contenido = new
        {
            Archivos = new { ClaveBase64 = claves[ClaveArchivos] },
            Jwt = new { ClaveFirma = claves[ClaveJwt] },
        };
        File.WriteAllText(ruta, JsonSerializer.Serialize(contenido, new JsonSerializerOptions { WriteIndented = true }));
    }
}
