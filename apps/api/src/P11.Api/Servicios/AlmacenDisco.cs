using Microsoft.Extensions.Options;
using P11.Api.Configuracion;

namespace P11.Api.Servicios;

public interface IAlmacenArchivos
{
    /// <summary>Guarda el contenido y devuelve el nombre del archivo en disco.</summary>
    Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default);
    Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default);

    /// <summary>Borra el archivo si existe (al eliminar un usuario). Por defecto no hace nada.</summary>
    Task EliminarAsync(string nombre, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Guarda los bytes tal como llegan en la carpeta de archivos. No cifra: de eso se encarga AlmacenCifrado.</summary>
public class AlmacenDisco : IAlmacenArchivos
{
    private readonly string _carpeta;

    public AlmacenDisco(IOptions<ArchivosOptions> opciones, IHostEnvironment env)
    {
        var carpeta = opciones.Value.Carpeta;
        _carpeta = Path.IsPathRooted(carpeta) ? carpeta : Path.Combine(env.ContentRootPath, carpeta);
        Directory.CreateDirectory(_carpeta);
    }

    public async Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default)
    {
        var nombre = $"{Guid.NewGuid():N}.bin";
        await File.WriteAllBytesAsync(RutaDe(nombre), contenido, ct);
        return nombre;
    }

    public Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default) =>
        File.ReadAllBytesAsync(RutaDe(nombre), ct);

    public Task EliminarAsync(string nombre, CancellationToken ct = default)
    {
        File.Delete(RutaDe(nombre)); // no falla si el archivo ya no está
        return Task.CompletedTask;
    }

    private string RutaDe(string nombre) => Path.Combine(_carpeta, Path.GetFileName(nombre));
}
