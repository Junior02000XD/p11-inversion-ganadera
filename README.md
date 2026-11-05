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
apps/api/       API (backend)
apps/web/       Cliente web
apps/android/   Cliente Android
docs/patrones/  Mapa patrón de diseño → clase/archivo → requerimiento
docs/sprints/   Alcance y evidencia de cada sprint
```

El stack tecnológico está **por definir** por el equipo; cada carpeta de `apps/` se inicializa cuando se elija.

## Convenciones

- Idioma del dominio y la documentación: español (proyecto, ronda, cupo, participación, expediente, escenario, liquidación). IDs de requerimientos tal como en la ERS: `RF-xx`, `RNF-xx`.
- Una rama por historia o requerimiento (`feature/rf-02-login`), Pull Request a `main` revisado por otro integrante.
- Mensajes de commit en español, en imperativo y citando el requerimiento: `RF-02: bloquear la cuenta al 5.º intento fallido`.
- Nunca subir secretos: contraseñas, claves y cadenas de conexión van en archivos `.env` / `appsettings.Development.json` locales (ignorados por git).
