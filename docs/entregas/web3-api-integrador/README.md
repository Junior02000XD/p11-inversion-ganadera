# API Integrador — Programación Web 3

Vence el **jueves 20/11/2026 a las 7:00** y se entrega por Teams. Se sube un ZIP con:
- la Web API y sus migraciones, sin `bin`, `obj` ni `.vs`;
- un README con nombre del proyecto, versión de .NET, integrantes con su usuario de GitHub, enlace al repo, cómo configurar SQL Server con una cadena de ejemplo sin credenciales, cómo aplicar migraciones, cómo cargar los datos de prueba y cómo abrir Swagger;
- un PDF de evidencias.

La participación de cada integrante se mide por sus commits.

Plan escrito el 18/11/2026 para la sesión siguiente.

## Estado de la rúbrica

| Criterio (pts) | Dónde está | Estado |
|---|---|---|
| Entidades, claves y relación (8) | `Datos/AppDbContext.cs`. `ProyectoGanadero` (TPH engorde/lechería) → `Usuario`; `HistorialProyecto` → `ProyectoGanadero` | ✓ |
| Configuración de EF Core y migración aplicable (8) | SQL Server, `Datos/Migraciones/*_Inicial.cs` | ✓ |
| Datos de prueba y persistencia tras reiniciar (4) | `Datos/SemillaDatos.cs` | Usuarios ✓; proyectos → **Matías** |
| Listado y consulta por ID (6) | `GET /api/proyectos`, `GET /api/proyectos/{id}` | ✓ |
| POST con 201 y `Location` (6) | `POST /api/proyectos` | ✓ |
| PUT y DELETE (9) | `PUT` y `DELETE /api/proyectos/{id}` | ✓ |
| Recurso relacionado (4) | `GET /api/proyectos/{id}/historial` | ✓ |
| Separación modelos / controladores / DTO / datos (3) | `Dominio/`, `Controllers/`, `Contratos/`, `Datos/` | ✓ |
| DTO de entrada y salida (5) | `ProyectoEntradaDto` (entrada, con validaciones), `ProyectoDto` y `HistorialProyectoDto` (salida) | ✓ |
| Contexto inyectado y operaciones asíncronas (2) | constructor primario + `async`/`CancellationToken` | ✓ |
| Obligatorios y regla del dominio (5) | `[Required]`/`[Range]` + `Validar()` + State (solo se edita y borra un borrador) | ✓ |
| Referencias inválidas, 400 y 404 (5) | `productorId` inexistente → 400; id inexistente → 404 | ✓ (pruebas de Sergio en `ProyectosCrudTests.cs`, PR #1) |
| Swagger con rutas, parámetros, esquemas y respuestas (8) | `/swagger` + esquema Bearer | ✓ (códigos de respuesta de Brandon, PR #2) |
| Evidencias: éxito, errores y persistencia (7) | PDF (abajo) | **Pendiente** |
| ZIP limpio (3) + README reproducible (2) | script (abajo) + `apps/api/README.md` | ZIP **pendiente**; README → **Matías** |
| Commits de cada integrante (15) | ramas `aporte/<nombre>`, guía en [`aportes-equipo.md`](aportes-equipo.md) | Julio, Sergio y Brandon ✓; Matías **pendiente** |

## Siguiente sesión (miércoles 19/11 a la noche o jueves temprano)

1. **Unir los aportes**: revisar los PR de Sergio, Brandon y Matías y unirlos con «Create a merge commit». `dotnet test` en verde.
   - **Si algún aporte no llegó**: completar esa parte igual (pruebas, `ProducesResponseType`, semilla o README), para que la nota del grupo no pierda esos puntos. Avisarle a Julio que ese integrante queda sin commit.
2. **README de la entrega** (`apps/api/README.md`): comprobar que tenga todo lo que pide la consigna. Completar los usuarios de GitHub de los cuatro.
3. **PDF de evidencias** (`evidencias-api.pdf`, en esta carpeta). Guion de capturas con Brave headless y `puppeteer-core`, que se instala en el scratchpad (`npm i puppeteer-core@24.10.0`). Primero arrancar `docker start mssql-dev` + `docker compose up -d` + `dotnet run --launch-profile https`. Cada captura lleva un título «operación · código»:
   1. Swagger UI con la lista de endpoints y el esquema de `ProyectoEntradaDto`
   2. `POST /api/auth/login` · 200 (el token se pega en **Authorize**)
   3. `GET /api/proyectos` · 200
   4. `GET /api/proyectos/{id}` · 200
   5. `POST /api/proyectos` · 201, con el encabezado `Location` visible
   6. `PUT /api/proyectos/{id}` · 200
   7. `GET /api/proyectos/{id}/historial` · 200
   8. `DELETE /api/proyectos/{id}` · 204
   9. `POST /api/proyectos` sin campos · 400
   10. `POST /api/proyectos` (admin) con `productorId` inexistente · 400
   11. `GET /api/proyectos/{id inventado}` · 404
   12. `PUT` sobre un proyecto en revisión · 409 (regla del dominio)
   13. Reiniciar la API (detener y volver a arrancar) → `GET /api/proyectos` · 200 con los mismos datos

   Con «Try it out» de Swagger se ve la respuesta y el código en pantalla. Las capturas se arman en un PDF, una por página con su título. Copia en `/mnt/DATOS/01_Universidad/2026_S4/` (carpeta de Programación Web 3).
4. **ZIP**: desde la raíz del repo, con `git archive`, así salen solo los archivos versionados y nunca `bin`, `obj`, `.vs` ni secretos:
   ```bash
   git archive --format=zip -o /tmp/P11_API_Los-Cesaritos.zip HEAD apps/api global.json dotnet-tools.json docker-compose.yml
   zip -j /tmp/P11_API_Los-Cesaritos.zip docs/entregas/web3-api-integrador/evidencias-api.pdf
   unzip -l /tmp/P11_API_Los-Cesaritos.zip | grep -E "/(bin|obj|\.vs)/" && echo "ZIP SUCIO" || echo "ZIP limpio"
   ```
   Verificar que el README de `apps/api` esté adentro y tenga el enlace al repo. Copiar el ZIP a la carpeta de Programación Web 3 en DATOS. Julio lo sube a Teams.
5. Commit del PDF y push.
