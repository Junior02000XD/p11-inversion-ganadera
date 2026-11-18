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
apps/api/           API (backend) y sus pruebas
apps/web/           Cliente web
apps/android/       Cliente Android
docs/requisitos/    ERS 2.0 y especificación de requerimientos
docs/analisis/      PESTEL, Business Model Canvas y Modelo de Negocio
docs/procesos/      BPMN de los cuatro procesos (AS-IS → TO-BE)
docs/diagramas/     Diagramas UML (.mdj de StarUML), sus figuras PNG y los mockups
docs/diseno/        Documento de Diseño entregado
docs/patrones/      Mapa patrón de diseño → clase/archivo → requerimiento, informes y guías
docs/sprints/       Planificación, alcance y evidencia de cada sprint
docker-compose.yml  Correo de prueba (Mailpit) para desarrollo
```

## Stack tecnológico

| Parte | Tecnología | Estado |
|---|---|---|
| API (`apps/api/`) | ASP.NET Core (.NET 10) | Confirmado |
| Base de datos | SQL Server 2022 + EF Core | Confirmado (cambió de PostgreSQL el 18/11/2026, lo pide Programación Web 3) |
| Autenticación | ASP.NET Core Identity + JWT | Confirmado |
| Cliente web (`apps/web/`) | React + Vite + JavaScript | Confirmado |
| Cliente Android (`apps/android/`) | Java nativo (Retrofit + Room + WorkManager) | Confirmado; se inicia en el Sprint 3 |

## Cómo levantar todo en local

Hace falta .NET SDK 10, Node.js 20.19 o superior, Docker y un SQL Server en `localhost:1433` (en Windows, SQL Server Express o Developer; en Linux o Mac, el contenedor de abajo). La cadena de conexión con tu clave se guarda una sola vez con `dotnet user-secrets` (ver [`apps/api/README.md`](apps/api/README.md)); la API crea la base y aplica las migraciones sola al arrancar. Cada comando va en su propia terminal, desde la raíz del repo:

```bash
docker run -d --name p11-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<una clave fuerte>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest   # solo la primera vez
docker compose up -d                                           # Mailpit (correos de prueba en :8025)
cd apps/api/src/P11.Api && dotnet run --launch-profile https   # API en https://localhost:7080
cd apps/web && npm install && npm run dev                      # web en http://localhost:5173
```

Fuera de la cadena de conexión no hay que crear archivos de configuración: en desarrollo la API genera sus claves y crea los usuarios internos. Swagger UI queda en https://localhost:7080/swagger. La primera vez en cada máquina, corre `dotnet dev-certs https --trust`. Detalles, puertos ocupados y correo real: [`apps/api/README.md`](apps/api/README.md). Las pruebas se corren con `cd apps/api && dotnet test`.

Estado y guion de la demo del Sprint 1: [`docs/sprints/sprint-1/`](docs/sprints/sprint-1/README.md); plan del Sprint 2: [`docs/sprints/sprint-2/`](docs/sprints/sprint-2/README.md). El menú de la web tiene una pestaña por caso de uso: [`apps/web/src/casosDeUso.js`](apps/web/src/casosDeUso.js). Patrones de diseño: [`docs/patrones/README.md`](docs/patrones/README.md). Diagramas: [`docs/diagramas/README.md`](docs/diagramas/README.md).

## Convenciones

- Idioma del dominio y la documentación: español (proyecto, ronda, cupo, participación, expediente, escenario, liquidación). IDs de requerimientos tal como en la ERS: `RF-xx`, `RNF-xx`.
- Una rama por historia o requerimiento (`feature/rf-02-login`), Pull Request a `main` revisado por otro integrante.
- Mensajes de commit en español, en imperativo y citando el requerimiento: `RF-02: bloquear la cuenta al 5.º intento fallido`.
- Nunca subir secretos: las claves y contraseñas reales van en `dotnet user-secrets` o en variables de entorno, nunca en archivos versionados. `appsettings.Development.json` solo tiene valores del entorno local de Docker.
- Documentación en `docs/`: nombres en minúscula con guiones, sin fecha, materia ni autor, y una sola versión por archivo (la historia la lleva git). Reglas en [`docs/README.md`](docs/README.md).
