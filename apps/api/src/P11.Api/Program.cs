using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using P11.Api.Configuracion;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Servicios;
using P11.Api.Servicios.Avisos;
using P11.Api.Servicios.Documentos;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    ClavesDesarrollo.Agregar(builder.Configuration, builder.Environment.ContentRootPath);

// Configuración inyectada (en lugar de Singleton): cada sección se valida al arrancar
// y llega a los servicios como IOptions<T>.
AgregarOpciones<ReglasSeguridadOptions>(ReglasSeguridadOptions.Seccion);
AgregarOpciones<OtpOptions>(OtpOptions.Seccion);
AgregarOpciones<JwtOptions>(JwtOptions.Seccion);
AgregarOpciones<CorreoOptions>(CorreoOptions.Seccion);
AgregarOpciones<VerificacionOptions>(VerificacionOptions.Seccion);
AgregarOpciones<ArchivosOptions>(ArchivosOptions.Seccion)
    .Validate(o => ArchivosOptions.EsClaveAes256(o.ClaveBase64), "Archivos:ClaveBase64 debe ser una clave AES-256 en base64 (32 bytes).");

// En las pruebas de integración (entorno Testing) la base la registra la fábrica de pruebas.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Principal")));

builder.Services.AddIdentityCore<Usuario>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// RF-01, RF-02: correo único y confirmado, bloqueo tras N intentos fallidos.
builder.Services.AddOptions<IdentityOptions>()
    .Configure<IOptions<ReglasSeguridadOptions>>((identity, reglas) =>
    {
        var r = reglas.Value;
        identity.User.RequireUniqueEmail = true;
        identity.SignIn.RequireConfirmedEmail = true;
        identity.Password.RequiredLength = r.LongitudMinimaContrasena;
        identity.Lockout.AllowedForNewUsers = true;
        identity.Lockout.MaxFailedAccessAttempts = r.MaxIntentosFallidos;
        identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(r.MinutosBloqueo);
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, opciones) =>
    {
        var jwt = opciones.Value;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = TokenJwtService.ClaveDeFirma(jwt),
            RoleClaimType = TokenJwtService.ClaimRol,
            NameClaimType = TokenJwtService.ClaimCorreo,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton(TimeProvider.System);

// Decorator: el cifrado (RNF-02) envuelve al almacén en disco, y los reintentos envuelven al SMTP.
builder.Services.AddSingleton<AlmacenDisco>();
builder.Services.AddSingleton<IAlmacenArchivos>(sp =>
    new AlmacenCifrado(sp.GetRequiredService<AlmacenDisco>(), sp.GetRequiredService<IOptions<ArchivosOptions>>()));
builder.Services.AddScoped<CorreoSmtpService>();
builder.Services.AddScoped<ICorreoService>(sp =>
    new CorreoConReintentos(sp.GetRequiredService<CorreoSmtpService>(), sp.GetRequiredService<ILogger<CorreoConReintentos>>()));

// Bridge: hoy los avisos al usuario salen por correo; Telegram o push serían otro ICanalAviso.
builder.Services.AddScoped<ICanalAviso, CanalCorreo>();

// Proxy: quien pide IVisorDocumentos recibe el proxy, que revisa el acceso antes de abrir.
builder.Services.AddScoped<VisorDocumentos>();
builder.Services.AddScoped<IVisorDocumentos, VisorDocumentosProxy>();

builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<VerificacionService>();
builder.Services.AddScoped<TokenJwtService>();
builder.Services.AddScoped<IVerificadorIdentidad, VerificadorIdentidadSimulado>();

// Factory Method (RF-07): un creador concreto por línea; el selector elige según la ficha.
builder.Services.AddSingleton<CreadorProyecto, CreadorProyectoEngorde>();
builder.Services.AddSingleton<CreadorProyecto, CreadorProyectoLecheria>();
builder.Services.AddSingleton<SelectorCreadorProyecto>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

await PrepararBaseDeDatosAsync(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // documentación interactiva en /scalar
}

app.UseHttpsRedirection(); // RNF-02
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

OptionsBuilder<T> AgregarOpciones<T>(string seccion) where T : class =>
    builder.Services.AddOptions<T>()
        .Bind(builder.Configuration.GetSection(seccion))
        .ValidateDataAnnotations()
        .ValidateOnStart();

static async Task PrepararBaseDeDatosAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
    await SemillaDatos.CargarAsync(scope.ServiceProvider, app.Configuration);
}

public partial class Program;
