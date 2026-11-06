using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;

namespace P11.Api.Servicios;

public interface IAlmacenArchivos
{
    /// <summary>Guarda el contenido cifrado y devuelve el nombre del archivo en disco.</summary>
    Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default);
    Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default);
}

/// <summary>
/// RNF-02: los documentos de identidad se guardan cifrados con AES-256-GCM.
/// Formato en disco: nonce (12 bytes) · tag (16 bytes) · texto cifrado. Sin la clave no se pueden abrir.
/// </summary>
public class AlmacenCifrado : IAlmacenArchivos
{
    private const int TamNonce = 12, TamTag = 16;
    private readonly byte[] _clave;
    private readonly string _carpeta;

    public AlmacenCifrado(IOptions<ArchivosOptions> opciones, IHostEnvironment env)
    {
        _clave = Convert.FromBase64String(opciones.Value.ClaveBase64);
        if (_clave.Length != 32) throw new InvalidOperationException("Archivos:ClaveBase64 debe ser una clave de 32 bytes (AES-256).");
        _carpeta = Path.IsPathRooted(opciones.Value.Carpeta) ? opciones.Value.Carpeta : Path.Combine(env.ContentRootPath, opciones.Value.Carpeta);
        Directory.CreateDirectory(_carpeta);
    }

    public string RutaDe(string nombre) => Path.Combine(_carpeta, Path.GetFileName(nombre));

    public async Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default)
    {
        var nonce = RandomNumberGenerator.GetBytes(TamNonce);
        var tag = new byte[TamTag];
        var cifrado = new byte[contenido.Length];
        using (var aes = new AesGcm(_clave, TamTag))
            aes.Encrypt(nonce, contenido, cifrado, tag);

        var nombre = $"{Guid.NewGuid():N}.bin";
        await using var fs = File.Create(RutaDe(nombre));
        await fs.WriteAsync(nonce, ct);
        await fs.WriteAsync(tag, ct);
        await fs.WriteAsync(cifrado, ct);
        return nombre;
    }

    public async Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default)
    {
        var datos = await File.ReadAllBytesAsync(RutaDe(nombre), ct);
        var nonce = datos.AsSpan(0, TamNonce);
        var tag = datos.AsSpan(TamNonce, TamTag);
        var cifrado = datos.AsSpan(TamNonce + TamTag);
        var plano = new byte[cifrado.Length];
        using var aes = new AesGcm(_clave, TamTag);
        aes.Decrypt(nonce, cifrado, tag, plano);
        return plano;
    }
}
