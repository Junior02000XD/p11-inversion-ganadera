# P11 — Plataforma de Inversión Ganadera

API REST para el Proyecto 11, desarrollada con ASP.NET Core y .NET 10, Entity Framework Core, SQL Server, ASP.NET Core Identity y JWT.

Repositorio: [github.com/Junior02000XD/p11-inversion-ganadera](https://github.com/Junior02000XD/p11-inversion-ganadera)

## Integrantes

| Integrante | Usuario de GitHub |
|---|---|
| Julio César Cruz Kübber | `Junior02000XD` |
| Sergio Ricardo Aguilera Pantoja | `sergioagulerapantoja-ctrl` |
| Jael Brandon Quiroga Vedia | `brandonquiroga908-netizen` |
| Matías Raldes León | `RLM31` |

## Requisitos

- .NET SDK 10.0.x (el `global.json` del repositorio selecciona la versión compatible).
- Docker con Docker Compose: el `docker-compose.yml` de la raíz levanta SQL Server 2022 (`localhost:1433`) y Mailpit, que recibe los correos de desarrollo.

## Configurar y ejecutar

Paso a paso desde cero y solución de errores frecuentes: [instalación en una máquina nueva](../../README.md#instalación-en-una-máquina-nueva). En resumen, desde la raíz del repositorio:

```bash
docker compose up -d --wait                            # SQL Server y Mailpit; espera a que estén listos
dotnet dev-certs https --trust                         # una vez por máquina
dotnet run --project apps/api --launch-profile https   # crea la base, aplica migraciones y carga la semilla
```

No hace falta configurar nada. En Development, la cadena de conexión de `appsettings.Development.json` apunta al SQL Server del compose con su clave de prueba (`P11.Pruebas-2026`; el contenedor solo escucha en `127.0.0.1`), y la API genera sus claves de cifrado y de firma de tokens en `App_Data/claves-desarrollo.json` (ignorado por git).

Para usar otro SQL Server, otra clave u otro puerto, se guarda la cadena con `user-secrets`, que tiene prioridad sobre `appsettings.Development.json` y no se sube al repositorio. Por ejemplo, con SQL Server instalado en Windows y autenticación integrada (y solo Mailpit con `docker compose up -d mailpit`):

```powershell
cd apps/api
dotnet user-secrets set "ConnectionStrings:Principal" "Server=localhost;Database=P11;Trusted_Connection=True;TrustServerCertificate=True"
```

La API también acepta la variable de entorno `ConnectionStrings__Principal`. Fuera de Development no se genera nada: `Jwt:ClaveFirma` (base64, 32 bytes o más) y `Archivos:ClaveBase64` (AES-256, 32 bytes) tienen que venir de user-secrets o de variables de entorno, o la API no arranca.

Las migraciones se aplican solas al arrancar. Para aplicarlas a mano, desde la raíz: `dotnet tool restore` y `dotnet ef database update --project apps/api`.

- API: <https://localhost:7080>
- Swagger UI: <https://localhost:7080/swagger>
- Scalar: <https://localhost:7080/scalar>
- Mailpit: <http://localhost:8025>

## Datos de prueba

En entorno Development, la semilla crea los roles y estas cuentas. Son credenciales solo para desarrollo local; cámbialas si las usas fuera de un entorno de prueba.

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@p11.local` | `Admin.p11-2026` |
| Analista | `analista@p11.local` | `Analista.p11-2026` |
| Productor | `productor@p11.local` | `Productor.p11-2026` |
| Inversionista | `inversionista@p11.local` | `Inversionista.p11-2026` |

El productor de prueba queda con identidad verificada. Si la tabla de proyectos está vacía, se crean «Engorde Chiquitania 2026» (Engorde, San Ignacio de Velasco) y «Tambo Warnes» (Lechería, Warnes). La semilla no los duplica al reiniciar la API.

Para autenticar una solicitud en Swagger, ejecutar `POST /api/auth/login` con el correo y contraseña de una cuenta, copiar el `token` de la respuesta, pulsar **Authorize** y pegar el token. La API ya configura el esquema Bearer.

## Pruebas

Las pruebas están en un proyecto aparte, `tests/api` (fuera de esta carpeta, para que no se compilen dentro de la API). Desde la raíz:

```powershell
dotnet test tests/api
```

Las pruebas de integración usan una base de datos en memoria y no requieren SQL Server.

## Evidencias

Las pruebas de la API hechas desde Swagger UI (200, 201, 204, 400, 404, 409 y la consulta después de reiniciar) están en [`../../docs/entregas/web3-api-integrador/evidencias-api.pdf`](../../docs/entregas/web3-api-integrador/evidencias-api.pdf).
