using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;

namespace P11.Api.Servicios;

/// <summary>
/// Decorator (RNF-02): envuelve a otro almacén y cifra con AES-256-GCM antes de guardar y descifra al leer.
/// Formato guardado: nonce (12 bytes) · tag (16 bytes) · texto cifrado. Sin la clave no se pueden abrir.
/// </summary>
public class AlmacenCifrado : IAlmacenArchivos
{
    private const int TamNonce = 12, TamTag = 16;
    private readonly IAlmacenArchivos _interno;
    private readonly byte[] _clave;

    public AlmacenCifrado(IAlmacenArchivos interno, IOptions<ArchivosOptions> opciones)
    {
        _interno = interno;
        _clave = Convert.FromBase64String(opciones.Value.ClaveBase64);
        if (_clave.Length != 32) throw new InvalidOperationException("Archivos:ClaveBase64 debe ser una clave de 32 bytes (AES-256).");
    }

    public Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default)
    {
        var paquete = new byte[TamNonce + TamTag + contenido.Length];
        var nonce = paquete.AsSpan(0, TamNonce);
        RandomNumberGenerator.Fill(nonce);
        using (var aes = new AesGcm(_clave, TamTag))
            aes.Encrypt(nonce, contenido, paquete.AsSpan(TamNonce + TamTag), paquete.AsSpan(TamNonce, TamTag));

        return _interno.GuardarAsync(paquete, ct);
    }

    public async Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default)
    {
        var paquete = await _interno.LeerAsync(nombre, ct);
        var cifrado = paquete.AsSpan(TamNonce + TamTag);
        var plano = new byte[cifrado.Length];
        using var aes = new AesGcm(_clave, TamTag);
        aes.Decrypt(paquete.AsSpan(0, TamNonce), cifrado, paquete.AsSpan(TamNonce, TamTag), plano);
        return plano;
    }
}
