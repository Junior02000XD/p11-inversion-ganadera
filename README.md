# p11-inversion-ganadera

Plataforma digital de inversión ganadera (Proyecto 11): conecta inversionistas urbanos con productores de engorde y lechería que necesitan capital sin garantías bancarias. Android + web.

Equipo **Los Cesaritos** — Universidad Privada del Valle, Santa Cruz, 2026:
Sergio Ricardo Aguilera Pantoja (Scrum Master), Matías Raldes León (Product Owner), Julio César Cruz Kübber y Jael Brandon Quiroga Vedia (Desarrolladores).

## Materias que cubre este repositorio

| Materia | Qué vive acá |
|---|---|
| Proyecto de Sistemas 1 | El producto por sprints (S1 06/11, S2 20/11, S3 04/12, defensa 11/12) — `docs/sprints/` |
| Programación Web 3 | La API del proyecto — `apps/api/` |
| Ingeniería de Software | Patrones de diseño implementados en el código — `docs/patrones/` |

## Estructura

```
apps/api/           API (backend) y sus pruebas
apps/web/           Cliente web
apps/android/       Cliente Android
docs/patrones/      Mapa patrón de diseño → clase/archivo → requerimiento
docs/sprints/       Alcance y evidencia de cada sprint
docker-compose.yml  Base de datos y correo de prueba para desarrollo
```

## Stack tecnológico

| Parte | Tecnología | Estado |
|---|---|---|
| API (`apps/api/`) | ASP.NET Core (.NET 10) | Confirmado |
| Base de datos | PostgreSQL | Confirmado |
| Autenticación | ASP.NET Core Identity + JWT | Confirmado |
| Cliente web (`apps/web/`) | React + Vite + TypeScript | Confirmado |
| Cliente Android (`apps/android/`) | Java nativo (Retrofit + Room + WorkManager) | Confirmado; se inicia en el Sprint 3 |

## Cómo levantar todo en local

Hace falta .NET SDK 10, Node.js 20.19 o superior, y Docker. Cada comando va en su propia terminal, desde la raíz del repo:

```bash
docker compose up -d                                           # PostgreSQL (5433) + Mailpit (correos de prueba en :8025)
cd apps/api/src/P11.Api && dotnet run --launch-profile https   # API en https://localhost:7080
cd apps/web && npm install && npm run dev                      # web en http://localhost:5173
```

No hay que crear archivos de configuración: en desarrollo la API genera sus claves y crea los usuarios internos. La primera vez en cada máquina, corre `dotnet dev-certs https --trust`. Detalles, puertos ocupados y correo real: [`apps/api/README.md`](apps/api/README.md). Las pruebas se corren con `cd apps/api && dotnet test`.

Estado y guion de la demo del Sprint 1: [`docs/sprints/sprint-1.md`](docs/sprints/sprint-1.md). Patrones de diseño: [`docs/patrones/README.md`](docs/patrones/README.md).

## Convenciones

- Idioma del dominio y la documentación: español (proyecto, ronda, cupo, participación, expediente, escenario, liquidación). IDs de requerimientos tal como en la ERS: `RF-xx`, `RNF-xx`.
- Una rama por historia o requerimiento (`feature/rf-02-login`), Pull Request a `main` revisado por otro integrante.
- Mensajes de commit en español, en imperativo y citando el requerimiento: `RF-02: bloquear la cuenta al 5.º intento fallido`.
- Nunca subir secretos: las claves y contraseñas reales van en `dotnet user-secrets` o en variables de entorno, nunca en archivos versionados. `appsettings.Development.json` solo tiene valores del entorno local de Docker.
