# p11-inversion-ganadera

Plataforma digital de inversión ganadera (Proyecto 11): conecta inversionistas urbanos con productores de engorde y lechería que necesitan capital sin garantías bancarias. Android + web.

Equipo **Los Cesaritos** — Universidad Privada del Valle, Santa Cruz, 2026:
Sergio Ricardo Aguilera Pantoja (Scrum Master), Matías Raldes León (Product Owner), Julio César Cruz Kübber y Jael Brandon Quiroga Vedia (Desarrolladores).

## Materias que cubre este repositorio

| Materia | Qué vive acá |
|---|---|
| Proyecto de Sistemas 1 | El producto por sprints (S1 06/11, S2 20/11, S3 04/12, defensa 11/12) — `docs/sprints/` |
| Programación Web 3 | La API del proyecto — `apps/api/` |
| Ingeniería de Software | Requisitos, diagramas UML y patrones de diseño implementados en el código — `docs/requisitos/`, `docs/diagramas/`, `docs/patrones/` |

Toda la documentación (ERS, diagramas, mockups, informes, guías, entregas de cada sprint) está en [`docs/`](docs/README.md), cada archivo en su versión vigente.

## Estructura

```
apps/api/           API (backend)
apps/web/           Cliente web
apps/android/       Cliente Android
docs/requisitos/    ERS 2.0 y especificación de requerimientos
docs/analisis/      PESTEL, Business Model Canvas y Modelo de Negocio
docs/procesos/      BPMN de los cuatro procesos (AS-IS → TO-BE)
docs/diagramas/     Diagramas UML (.mdj de StarUML), sus figuras PNG y los mockups
docs/diseno/        Documento de Diseño entregado
docs/patrones/      Mapa patrón de diseño → clase/archivo → requerimiento, informes y guías
docs/sprints/       Planificación, alcance y evidencia de cada sprint
docs/guia/          Guía de estudio del proyecto, con capturas
tests/api/          Pruebas automáticas de la API (dotnet test tests/api)
P11.sln             Solución de .NET: abre la API y las pruebas juntas en el IDE
docker-compose.yml  Entorno local: SQL Server y correo de prueba (Mailpit)
.env.example        Plantilla del .env opcional: cambiar la clave de SQL Server o los puertos
```

## Stack tecnológico

| Parte | Tecnología | Estado |
|---|---|---|
| API (`apps/api/`) | ASP.NET Core (.NET 10) | Confirmado |
| Base de datos | SQL Server 2022 + EF Core | Confirmado (cambió de PostgreSQL el 18/11/2026, lo pide Programación Web 3) |
| Autenticación | ASP.NET Core Identity + JWT | Confirmado |
| Cliente web (`apps/web/`) | React + Vite + JavaScript | Confirmado |
| Cliente Android (`apps/android/`) | Java nativo (Retrofit + Room + WorkManager) | Confirmado; se inicia en el Sprint 3 |

## Instalación en una máquina nueva

No hay que crear archivos de configuración ni claves: la base de datos de prueba, la clave de SQL Server, las claves de la API y los usuarios de prueba vienen listos para desarrollo. Los comandos sirven igual en PowerShell (Windows) y en la terminal de Linux o macOS.

### 1. Instalar las herramientas (una sola vez)

| Herramienta | Para qué | Cómo comprobar que está |
|---|---|---|
| [Git](https://git-scm.com/downloads) | Clonar el repositorio | `git --version` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (en Linux, Docker Engine con el plugin Compose) | SQL Server y el correo de prueba, sin instalarlos | `docker compose version` |
| [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) | La API | `dotnet --version` → `10.0.x` |
| [Node.js 22 LTS](https://nodejs.org/) (o 20.19 o superior) | La web | `node -v` |

En Windows, **Docker Desktop tiene que estar abierto** (el ícono de la ballena quieto en la bandeja) antes de usar `docker`.

### 2. Clonar

```bash
git clone https://github.com/Junior02000XD/p11-inversion-ganadera.git
cd p11-inversion-ganadera
```

### 3. Levantar la base de datos y el correo de prueba

```bash
docker compose up -d --wait
```

La primera vez descarga las imágenes (≈1,5 GB) y tarda unos minutos. Termina cuando muestra `p11-sqlserver Healthy` y `p11-mailpit Healthy`. Crea dos contenedores:

- `p11-sqlserver`: SQL Server 2022 en `localhost:1433`, usuario `sa`, clave de prueba `P11.Pruebas-2026`. Los datos quedan en el volumen `p11-sqlserver-datos`.
- `p11-mailpit`: un buzón de prueba. Los correos que manda la API (códigos OTP, avisos) se leen en <http://localhost:8025>. No sale nada a internet.

### 4. Arrancar la API

```bash
dotnet dev-certs https --trust                          # una sola vez por máquina; acepta el aviso
dotnet run --project apps/api --launch-profile https
```

En el primer arranque, la API crea la base `P11`, aplica las migraciones, genera sus claves de desarrollo (en `apps/api/App_Data/`, ignorada por git) y carga los usuarios y proyectos de prueba. Está lista cuando aparece `Now listening on: https://localhost:7080`.

- Swagger UI: <https://localhost:7080/swagger>
- Scalar (la misma documentación, con otro diseño): <https://localhost:7080/scalar>

### 5. Arrancar la web (en otra terminal)

```bash
cd apps/web
npm install        # solo la primera vez o si cambia package.json
npm run dev
```

Abrir <http://localhost:5173>. La web le pasa las llamadas `/api/...` a la API, así que la API tiene que estar corriendo.

### 6. Entrar

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@p11.local` | `Admin.p11-2026` |
| Analista | `analista@p11.local` | `Analista.p11-2026` |
| Productor | `productor@p11.local` | `Productor.p11-2026` |
| Inversionista | `inversionista@p11.local` | `Inversionista.p11-2026` |

Además se crean los proyectos «Engorde Chiquitania 2026» y «Tambo Warnes». Para probar la API en Swagger: `POST /api/auth/login` → *Try it out* → enviar un correo y su contraseña → copiar el `token` de la respuesta → botón **Authorize** → pegar el token (sin la palabra `Bearer`) → *Authorize*. Las rutas con candado ya responden.

### Pruebas automáticas

```bash
dotnet test tests/api
```

No necesitan Docker: usan una base de datos en memoria.

### Apagar, volver a empezar

```bash
docker compose stop        # apaga los contenedores; los datos se conservan
docker compose up -d --wait   # los vuelve a prender
docker compose down -v     # borra contenedores y base: el próximo arranque de la API la crea de cero
```

### Si algo falla

| Lo que aparece | Causa | Solución |
|---|---|---|
| `error during connect` / `Cannot connect to the Docker daemon` | Docker no está corriendo | Abrir Docker Desktop y esperar a que termine de iniciar |
| `Bind for 127.0.0.1:1433 failed: port is already allocated` | Ya hay un SQL Server en ese puerto (pasa en Windows con SQL Server Express instalado) | Crear un archivo `.env` en la raíz con `P11_PUERTO_SQL=1434`, volver a correr `docker compose up -d --wait` y guardar la cadena con ese puerto: `cd apps/api` y `dotnet user-secrets set "ConnectionStrings:Principal" "Server=localhost,1434;Database=P11;User Id=sa;Password=P11.Pruebas-2026;TrustServerCertificate=True"` |
| `Login failed for user 'sa'` | La API usa otra clave que la del contenedor: un `.env` o user-secrets viejos de una instalación anterior | Ver qué guardaste con `dotnet user-secrets list --project apps/api` y borrarlo con `dotnet user-secrets clear --project apps/api`; si el volumen se creó con otra clave, `docker compose down -v` y volver al paso 3 |
| `A network-related or instance-specific error` o `pre-login handshake` al arrancar la API | SQL Server todavía no terminó de arrancar | Usar siempre `--wait` en el paso 3; si no, esperar unos segundos y volver a correr la API |
| El navegador dice «La conexión no es privada» | Falta confiar en el certificado de desarrollo | `dotnet dev-certs https --trust` y reiniciar el navegador |
| `The command could not be loaded` / `requires .NET SDK 10` | Hay otro SDK de .NET | Instalar el SDK 10 (`dotnet --list-sdks` debe mostrar un `10.0.x`) |
| `npm run dev` pide otra versión de Node | Node demasiado viejo | Instalar Node 22 LTS |
| La web carga pero el login dice «No se pudo conectar» | La API no está corriendo | Arrancarla (paso 4) en otra terminal |
| `p11-sqlserver` se reinicia o queda `unhealthy` | Poca memoria libre o Mac con chip Apple | Cerrar programas pesados; en Mac con chip M, activar en Docker Desktop *Use Rosetta for x86/amd64 emulation* |

En Windows también sirve un SQL Server Express o Developer instalado en lugar del contenedor. En ese caso se levanta solo el correo, con `docker compose up -d mailpit`, y se guarda la cadena de conexión con `dotnet user-secrets` (ver [`apps/api/README.md`](apps/api/README.md)).

### Más sobre el proyecto

- Guía de la API: [`docs/guia/guia-api.html`](docs/guia/guia-api.html).
- Guía de estudio del proyecto completo: [`docs/guia/guia-de-estudio.html`](docs/guia/guia-de-estudio.html).
- Estado y guion de la demo: [Sprint 1](docs/sprints/sprint-1/README.md) y [Sprint 2](docs/sprints/sprint-2/README.md).
- El menú de la web tiene una pestaña por caso de uso: [`apps/web/src/casosDeUso.js`](apps/web/src/casosDeUso.js).
- Patrones de diseño: [`docs/patrones/README.md`](docs/patrones/README.md). Diagramas: [`docs/diagramas/README.md`](docs/diagramas/README.md).

## Convenciones

- Idioma del dominio y la documentación: español (proyecto, ronda, cupo, participación, expediente, escenario, liquidación). IDs de requerimientos tal como en la ERS: `RF-xx`, `RNF-xx`.
- Una rama por historia o requerimiento (`feature/rf-02-login`), Pull Request a `main` revisado por otro integrante.
- Mensajes de commit en español, en imperativo y citando el requerimiento: `RF-02: bloquear la cuenta al 5.º intento fallido`.
- Nunca subir secretos: las claves y contraseñas reales van en `dotnet user-secrets` o en variables de entorno, nunca en archivos versionados. `appsettings.Development.json` solo tiene valores del entorno local de Docker (incluida la clave de prueba del contenedor, que solo escucha en `127.0.0.1`).
- Documentación en `docs/`: nombres en minúscula con guiones, sin fecha, materia ni autor, y una sola versión por archivo (la historia la lleva git). Reglas en [`docs/README.md`](docs/README.md).
