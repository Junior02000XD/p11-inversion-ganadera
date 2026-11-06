# API — ASP.NET Core Web API (.NET 10)

Materia: Programación Web 3. Usa EF Core con PostgreSQL, ASP.NET Core Identity (roles y bloqueo) y JWT.

## Requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download). `global.json` acepta cualquier 10.0.x.
- Docker (Docker Desktop en Windows y macOS).

## Levantar en local
Desde la raíz del repo:

```bash
docker compose up -d                    # PostgreSQL + Mailpit
dotnet dev-certs https --trust          # una sola vez por máquina (certificado HTTPS de desarrollo)
cd apps/api/src/P11.Api
dotnet run --launch-profile https
```

No hace falta configurar nada más. Al arrancar en desarrollo, la API:
- aplica las migraciones a la base del `docker-compose.yml`;
- crea los roles y dos usuarios internos (`admin@p11.local` / `Admin.p11-2026` y `analista@p11.local` / `Analista.p11-2026`);
- la primera vez, genera sus claves de cifrado y de firma en `App_Data/claves-desarrollo.json`. Ese archivo está ignorado por git y cada máquina tiene las suyas.

Direcciones:
- API: https://localhost:7080; documentación interactiva en https://localhost:7080/scalar.
- Correos enviados (OTP y avisos): http://localhost:8025.

## Cambiar la configuración en tu máquina
`appsettings.Development.json` trae valores para el entorno del `docker-compose.yml` y no tiene secretos. Para cambiar algo solo en tu máquina, sin tocar archivos versionados, usa una de estas dos formas:

```bash
# user-secrets (se guarda fuera del repo)
dotnet user-secrets set "ConnectionStrings:Principal" "Host=localhost;Port=5434;Database=p11;Username=p11;Password=p11_local_dev"

# o variables de entorno (doble guion bajo en lugar de «:»)
export ConnectionStrings__Principal="Host=localhost;Port=5434;..."
```

Casos típicos:
- **Puerto 5433 ocupado**: crea un `.env` en la raíz con `P11_PUERTO_POSTGRES=5434`, vuelve a correr `docker compose up -d` y cambia `ConnectionStrings:Principal` como arriba.
- **Enviar correos reales** en lugar de Mailpit: configura `Correo:Host`, `Correo:Puerto`, `Correo:UsarSsl`, `Correo:Usuario` y `Correo:Contrasena`. Por ejemplo, Gmail con contraseña de aplicación: `smtp.gmail.com`, puerto 587, SSL activado.

Fuera de desarrollo (por ejemplo, un servidor de demo), la API **no** genera claves: hay que definir `Archivos__ClaveBase64` (32 bytes en base64), `Jwt__ClaveFirma` (32 caracteres o más) y `ConnectionStrings__Principal`. Si falta alguna, no arranca y dice cuál.

## Pruebas
```bash
cd apps/api
dotnet test
```
Son 44 pruebas y no necesitan Docker:
- unitarias de los patrones;
- de integración que levantan la API completa con base en memoria y correo simulado.

## Estructura
```
src/P11.Api/
  Dominio/            Entidades. Patrones: Proyectos/ (Factory Method), Expediente/ (Builder), Simulacion/ (Prototype)
  Configuracion/      Opciones validadas al arrancar (configuración inyectada en lugar de Singleton) y claves de desarrollo
  Datos/              AppDbContext (Identity + EF Core), migraciones, semilla
  Servicios/          OTP, correo SMTP, almacenamiento cifrado AES-GCM, verificador de identidad simulado, JWT
  Controllers/        Auth, Verificacion, DocumentosProductor, Proyectos, Analista, AdminUsuarios
  Contratos/          DTO de entrada y salida
tests/P11.Tests/
  Patrones/           Factory Method, Builder, Prototype, configuración inyectada
  Integracion/        RF-01…RF-07, RNF-01, RNF-02, RNF-07
```

## Migraciones
`dotnet-ef` viene como herramienta local del repo:

```bash
dotnet tool restore                                  # una vez, desde la raíz
cd apps/api/src/P11.Api
dotnet ef migrations add NombreDelCambio -o Datos/Migraciones
```
