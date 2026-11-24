using System.Security.Cryptography;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;
using P11.Api.Services;
using P11.Api.Services.Email;
using P11.Api.Services.Files;

namespace P11.Tests.Patterns;

/// <summary>Decorator: el cifrado se agrega a cualquier almacén y los reintentos a cualquier servicio de correo.</summary>
public class DecoratorTests
{
    private static readonly byte[] Contenido = "CI 1234567 SC - Maria Rojas"u8.ToArray();

    private class AlmacenEnMemoria : IAlmacenArchivos
    {
        public Dictionary<string, byte[]> Archivos { get; } = [];
        public Task<string> GuardarAsync(byte[] contenido, CancellationToken ct = default)
        {
            var nombre = $"{Archivos.Count + 1}.bin";
            Archivos[nombre] = contenido;
            return Task.FromResult(nombre);
        }
        public Task<byte[]> LeerAsync(string nombre, CancellationToken ct = default) => Task.FromResult(Archivos[nombre]);
    }

    private class CorreoQueFalla(int fallos) : ICorreoService
    {
        public int Llamadas { get; private set; }
        public Task EnviarAsync(string para, string asunto, string cuerpo, CancellationToken ct = default)
        {
            Llamadas++;
            return Llamadas <= fallos ? throw new IOException("SMTP no disponible") : Task.CompletedTask;
        }
    }

    private static IOptions<ArchivosOptions> Opciones(string carpeta = "") => Options.Create(new ArchivosOptions
    {
        Carpeta = carpeta,
        ClaveBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    });

    [Fact]
    public async Task Cifra_antes_de_pasar_al_almacen_que_envuelve()
    {
        var interno = new AlmacenEnMemoria();
        var almacen = new AlmacenCifrado(interno, Opciones());

        var nombre = await almacen.GuardarAsync(Contenido);

        var guardado = interno.Archivos[nombre];
        Assert.NotEqual(Contenido, guardado);
        Assert.Equal(Contenido.Length + 12 + 16, guardado.Length); // nonce y tag delante
        Assert.Equal(Contenido, await almacen.LeerAsync(nombre));
    }

    [Fact]
    public async Task Lee_los_archivos_guardados_antes_de_separar_el_cifrado()
    {
        // Mismo formato que escribía la versión anterior de AlmacenCifrado: nonce · tag · cifrado.
        var carpeta = Directory.CreateTempSubdirectory("p11-decorator-").FullName;
        try
        {
            var opciones = Opciones(carpeta);
            var clave = Convert.FromBase64String(opciones.Value.ClaveBase64);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var cifrado = new byte[Contenido.Length];
            using (var aes = new AesGcm(clave, 16)) aes.Encrypt(nonce, Contenido, cifrado, tag);
            await File.WriteAllBytesAsync(Path.Combine(carpeta, "anterior.bin"), [.. nonce, .. tag, .. cifrado]);

            var almacen = new AlmacenCifrado(new AlmacenDisco(opciones, new HostingEnvironment()), opciones);

            Assert.Equal(Contenido, await almacen.LeerAsync("anterior.bin"));
        }
        finally
        {
            Directory.Delete(carpeta, true);
        }
    }

    [Fact]
    public async Task Reintenta_el_correo_cuando_el_servidor_falla_un_momento()
    {
        var smtp = new CorreoQueFalla(fallos: 2);
        var correo = new CorreoConReintentos(smtp, NullLogger<CorreoConReintentos>.Instance, intentos: 3, esperaMs: 0);

        await correo.EnviarAsync("maria@test.bo", "Tu código", "123456");

        Assert.Equal(3, smtp.Llamadas);
    }

    [Fact]
    public async Task Se_rinde_despues_del_ultimo_intento()
    {
        var smtp = new CorreoQueFalla(fallos: 10);
        var correo = new CorreoConReintentos(smtp, NullLogger<CorreoConReintentos>.Instance, intentos: 3, esperaMs: 0);

        await Assert.ThrowsAsync<IOException>(() => correo.EnviarAsync("maria@test.bo", "Tu código", "123456"));
        Assert.Equal(3, smtp.Llamadas);
    }
}
