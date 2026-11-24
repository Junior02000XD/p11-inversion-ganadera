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
- SQL Server 2022 Express o Developer en `localhost:1433`. En Linux o macOS se puede usar el contenedor oficial (la clave la elige cada uno y no se sube al repositorio):

  ```bash
  docker run -d --name p11-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<una clave fuerte>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
  ```
- Docker y Docker Compose para Mailpit, que recibe los correos de desarrollo.

## Configurar y ejecutar

Desde la raíz del repositorio, iniciar Mailpit:

```bash
docker compose up -d
```

Configurar la cadena de conexión local sin guardar contraseñas en archivos versionados. En Windows, con autenticación integrada:

```powershell
cd apps/api
dotnet user-secrets set "ConnectionStrings:Principal" "Server=localhost;Database=P11;Trusted_Connection=True;TrustServerCertificate=True"
```

Con SQL Server configurado para autenticación SQL, usar una contraseña propia:

```powershell
dotnet user-secrets set "ConnectionStrings:Principal" "Server=localhost,1433;Database=P11;User Id=sa;Password=<tu_clave>;TrustServerCertificate=True"
```

Con `user-secrets` no se agrega la cadena ni la contraseña al repositorio. La aplicación también acepta la variable de entorno `ConnectionStrings__Principal`.

Restaurar la herramienta local desde la raíz y aplicar las migraciones (opcional si se inicia la API, porque las aplica automáticamente al arrancar):

```powershell
dotnet tool restore
cd apps/api
dotnet ef database update
```

Confiar en el certificado HTTPS de desarrollo una vez por máquina y ejecutar la API:

```powershell
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

- API: <https://localhost:7080>
- Swagger UI: <https://localhost:7080/swagger>
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
