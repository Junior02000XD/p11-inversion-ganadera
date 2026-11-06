using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using P11.Api.Datos;
using P11.Api.Servicios;

namespace P11.Tests.Integracion;

public record CorreoEnviado(string Para, string Asunto, string Cuerpo);

/// <summary>Reemplaza al SMTP: guarda los correos para que las pruebas lean el OTP y los avisos.</summary>
public class CorreoEnMemoria : ICorreoService
{
    public ConcurrentQueue<CorreoEnviado> Enviados { get; } = new();
    public Task EnviarAsync(string para, string asunto, string cuerpo, CancellationToken ct = default)
    {
        Enviados.Enqueue(new(para, asunto, cuerpo));
        return Task.CompletedTask;
    }
    public IEnumerable<CorreoEnviado> Para(string correo) => Enviados.Where(c => c.Para == correo);
}

/// <summary>Levanta la API real (Testing) con base en memoria, correo en memoria y claves de prueba.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Admin = "admin@test.local", Analista = "analista@test.local", ClaveInterna = "Interna.Prueba-2026";
    public CorreoEnMemoria Correo { get; } = new();
    public string CarpetaArchivos { get; } = Path.Combine(Path.GetTempPath(), "p11-pruebas-" + Guid.NewGuid().ToString("N"));
    public string ClaveArchivos { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _baseDatos = "p11-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Archivos:ClaveBase64", ClaveArchivos);
        builder.UseSetting("Archivos:Carpeta", CarpetaArchivos);
        builder.UseSetting("Jwt:ClaveFirma", "clave-de-firma-solo-para-pruebas-0123456789");
        builder.UseSetting("Semilla:Usuarios:0:Nombre", "Admin Prueba");
        builder.UseSetting("Semilla:Usuarios:0:Correo", Admin);
        builder.UseSetting("Semilla:Usuarios:0:Contrasena", ClaveInterna);
        builder.UseSetting("Semilla:Usuarios:0:Ci", "9000001");
        builder.UseSetting("Semilla:Usuarios:0:Rol", "Administrador");
        builder.UseSetting("Semilla:Usuarios:1:Nombre", "Ana Suárez");
        builder.UseSetting("Semilla:Usuarios:1:Correo", Analista);
        builder.UseSetting("Semilla:Usuarios:1:Contrasena", ClaveInterna);
        builder.UseSetting("Semilla:Usuarios:1:Ci", "9000002");
        builder.UseSetting("Semilla:Usuarios:1:Rol", "Analista");

        builder.ConfigureTestServices(s =>
        {
            s.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_baseDatos));
            s.RemoveAll<ICorreoService>();
            s.AddSingleton<ICorreoService>(Correo);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(CarpetaArchivos)) Directory.Delete(CarpetaArchivos, true);
    }
}

/// <summary>Atajos para los pasos que se repiten en las pruebas.</summary>
public static class Pasos
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static int _ci = 5_000_000;

    public static string CiNuevo() => Interlocked.Increment(ref _ci).ToString();
    public static string CorreoNuevo(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}@test.bo";

    public static Task<HttpResponseMessage> Registrar(HttpClient c, string correo, string rol, string? ci = null, string clave = "Clave.segura1") =>
        c.PostAsJsonAsync("/api/auth/registro", new { nombre = "María Rojas", correo, celular = "70011122", ci = ci ?? CiNuevo(), contrasena = clave, rol });

    public static string UltimoOtp(ApiFactory f, string correo) =>
        Regex.Match(f.Correo.Para(correo).Last(m => m.Asunto.Contains("código")).Asunto, @"\d{6}").Value;

    public static Task<HttpResponseMessage> Login(HttpClient c, string correo, string clave = "Clave.segura1") =>
        c.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = clave });

    /// <summary>Registra, confirma con el OTP y devuelve un cliente autenticado.</summary>
    public static async Task<HttpClient> UsuarioConfirmado(ApiFactory f, string rol, string? correo = null)
    {
        var c = f.CreateClient();
        correo ??= CorreoNuevo(rol.ToLowerInvariant());
        (await Registrar(c, correo, rol)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/auth/otp/verificar", new { correo, codigo = UltimoOtp(f, correo) })).EnsureSuccessStatusCode();
        return await Autenticado(f, correo);
    }

    public static async Task<HttpClient> Autenticado(ApiFactory f, string correo, string clave = "Clave.segura1")
    {
        var c = f.CreateClient();
        var r = await Login(c, correo, clave);
        r.EnsureSuccessStatusCode();
        var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    /// <summary>PNG mínimo pero válido, de más de 1 KB.</summary>
    public static byte[] Png(int lado = 48)
    {
        var raw = new List<byte>();
        for (var y = 0; y < lado; y++) { raw.Add(0); for (var x = 0; x < lado; x++) raw.Add((byte)(x * y)); }
        using var ms = new MemoryStream();
        void Chunk(string tipo, byte[] datos)
        {
            var t = System.Text.Encoding.ASCII.GetBytes(tipo);
            ms.Write(BitConverter.GetBytes(datos.Length).Reverse().ToArray());
            ms.Write(t); ms.Write(datos);
            ms.Write(BitConverter.GetBytes(System.IO.Hashing.Crc32.HashToUInt32([.. t, .. datos])).Reverse().ToArray());
        }
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = new byte[13];
        BitConverter.GetBytes(lado).Reverse().ToArray().CopyTo(ihdr, 0);
        BitConverter.GetBytes(lado).Reverse().ToArray().CopyTo(ihdr, 4);
        ihdr[8] = 8;
        Chunk("IHDR", ihdr);
        using (var z = new MemoryStream())
        {
            using (var zl = new System.IO.Compression.ZLibStream(z, System.IO.Compression.CompressionLevel.NoCompression, true)) zl.Write(raw.ToArray());
            Chunk("IDAT", z.ToArray());
        }
        Chunk("IEND", []);
        return ms.ToArray();
    }

    public static MultipartFormDataContent FormVerificacion(bool consentimiento, byte[]? ciDorso = null, string nombreSelfie = "selfie.png")
    {
        var f = new MultipartFormDataContent { { new StringContent(consentimiento ? "true" : "false"), "consentimiento" } };
        void Img(string campo, byte[] datos, string nombre)
        {
            var b = new ByteArrayContent(datos);
            b.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            f.Add(b, campo, nombre);
        }
        Img("ciFrente", Png(), "ci-frente.png");
        Img("ciDorso", ciDorso ?? Png(), "ci-dorso.png");
        Img("selfie", Png(), nombreSelfie);
        return f;
    }
}
