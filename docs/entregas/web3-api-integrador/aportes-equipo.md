# API Integrador (Programación Web 3): aportes del equipo

Entrega: **jueves 20/11/2026, 7:00** (Teams). La rúbrica da **15 puntos por integrante** según sus commits en este repositorio. Cada uno necesita **al menos un commit propio, con su usuario de GitHub y con cambios concretos** (código, pruebas o documentación técnica).

La base ya está hecha y probada: SQL Server, migración inicial, CRUD completo de `/api/proyectos` (GET, GET por id, POST 201 + `Location`, PUT, DELETE), el recurso relacionado `/api/proyectos/{id}/historial` y Swagger UI en `/swagger`. Lo de abajo completa la rúbrica. Cada tarea toca **archivos distintos**, así nadie se pisa con nadie.

**Plazo para abrir el pull request: miércoles 19/11, 21:00.** Después Julio lo revisa, lo une y arma el ZIP.

---

## Antes de empezar (todos)

1. Pasarle a Julio tu **usuario de GitHub** para que te invite como colaborador. Después, aceptar la invitación (llega por correo o aparece en github.com/notifications).
2. Configurar git con **el mismo correo que tiene tu cuenta de GitHub**. Si no coinciden, el commit no queda a tu nombre y no cuenta para la nota.
   ```bash
   git config --global user.name  "Tu Nombre"
   git config --global user.email "el-correo-de-tu-github@ejemplo.com"
   ```
3. Instalar el **SDK de .NET 10** (https://dotnet.microsoft.com/download/dotnet/10.0) y clonar:
   ```bash
   git clone https://github.com/Junior02000XD/p11-inversion-ganadera.git
   cd p11-inversion-ganadera
   git switch -c aporte/tu-nombre        # p. ej. aporte/sergio
   cd apps/api && dotnet test            # tiene que dar todo en verde antes de tocar nada
   ```
4. **SQL Server** (solo lo necesitan Brandon y Matías, para ver la API funcionando; las pruebas de Sergio no usan base):
   - **Windows**: SQL Server 2022 Express o Developer. La cadena de conexión:
     `Server=localhost;Database=P11;Trusted_Connection=True;TrustServerCertificate=True`
   - **Linux o Mac**: Docker.
     `docker run -d --name p11-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<una clave fuerte>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`
     La cadena de conexión: `Server=localhost,1433;Database=P11;User Id=sa;Password=<la misma clave>;TrustServerCertificate=True`
   - Guardar la cadena **sin subirla al repo**:
     ```bash
     cd apps/api/src/P11.Api
     dotnet user-secrets set "ConnectionStrings:Principal" "<tu cadena>"
     dotnet run --launch-profile https     # crea la base sola y abre en https://localhost:7080/swagger
     ```
   - Para entrar en Swagger: `POST /api/auth/login` con `admin@p11.local` / `Admin.p11-2026`. Copiar el `token`, tocar **Authorize** y pegarlo.
5. Al terminar:
   ```bash
   git add <tus archivos>
   git commit -m "Descripción en español de lo que hiciste"
   git push -u origin aporte/tu-nombre
   ```
   Después abrir un **pull request** hacia `main` en GitHub. Mejor varios commits chicos que uno gigante.

---

## Sergio: pruebas de integración del CRUD y de la administración

**Rúbrica que cubre:** «Validación y errores» (respuestas 400 y 404 verificables) y «Aportes concretos… a las pruebas».

**Archivo nuevo:** `apps/api/tests/P11.Tests/Integracion/ProyectosCrudTests.cs`. No toques los demás archivos de prueba.

Copiá el estilo de `ProyectosTests.cs`, que está en la misma carpeta. Ya tenés los ayudantes de `ApiFactory.cs`: `Pasos.UsuarioConfirmado(f, Roles.Productor)` te da un cliente con sesión, y `Pasos.Autenticado(f, ApiFactory.Admin, ApiFactory.ClaveInterna)` te da el administrador. Las pruebas usan base en memoria, así que no necesitás SQL Server.

Casos mínimos (un `[Fact]` cada uno):

| Operación | Esperado |
|---|---|
| `POST /api/proyectos` válido | **201**, y `Location` apunta a `/api/proyectos/{id}` |
| `POST` sin nombre ni línea | **400** con `errors.Nombre` y `errors.Linea` |
| `POST` del administrador con un `productorId` que no existe | **400** con `errors.ProductorId` |
| `GET /api/proyectos/{id}` con un id inventado | **404** |
| `PUT /api/proyectos/{id}` con nombre nuevo | **200**, y un GET posterior devuelve el nombre nuevo |
| `PUT` cambiando `linea` de Engorde a Lecheria | **400** con `errors.Linea` |
| `PUT` y `DELETE` sobre un id inventado | **404** |
| `DELETE /api/proyectos/{id}` de un borrador | **204**, y un GET posterior da **404** |
| Un productor intenta `GET` o `DELETE` del proyecto de **otro** productor | **404** |
| `GET /api/proyectos/{id}/historial` de un borrador recién creado | **200** con lista vacía |
| `DELETE /api/admin/usuarios/{id}` de un productor **con** proyectos | **409** |
| `DELETE /api/admin/usuarios/{id}` del propio administrador | **400** |

**Listo cuando:** `dotnet test` da todo en verde, con tus pruebas sumadas a las 87 que ya hay.

---

## Brandon: documentación de Swagger (respuestas y descripciones)

**Rúbrica que cubre:** «Swagger UI con rutas, parámetros, esquemas **y respuestas**» (8 pts). Hoy Swagger muestra las rutas y los esquemas, pero casi no muestra los códigos de respuesta.

**Archivos:** `apps/api/src/P11.Api/Controllers/ProyectosController.cs` (el recurso principal, el más importante), `AdminUsuariosController.cs` y `AdminBitacoraController.cs`. Si te da el tiempo, también `AuthController.cs`.

Qué hay que hacer en **cada acción**:
1. Agregar `[ProducesResponseType]` por cada código que la acción puede devolver. Mirá los `return` del método: `NotFound` es 404, `Conflict` es 409, `ValidationProblem` es 400, `CreatedAtAction` es 201 y `NoContent` es 204. Ejemplo:
   ```csharp
   [HttpPut("{id:guid}")]
   [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
   [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
   [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
   [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
   public async Task<ActionResult<ProyectoDto>> Actualizar(...)
   ```
2. En cada controlador con `[Authorize]`, agregar 401 y 403 a nivel de clase: `[ProducesResponseType(StatusCodes.Status401Unauthorized)]` y `[ProducesResponseType(StatusCodes.Status403Forbidden)]`.
3. Agregar `[Produces("application/json")]` en la clase del controlador.
4. Completar los comentarios `///` con `<response code="404">No existe o no es tuyo.</response>` y `<param name="id">…</param>`. Salen como texto en Swagger porque el proyecto genera la documentación XML.

**Listo cuando:** `dotnet build` termina sin advertencias nuevas, y en `https://localhost:7080/swagger` cada operación de *Proyectos* muestra todos sus códigos con descripción y esquema. Sacá una captura y mandala al grupo, porque va al PDF de evidencias.

---

## Matías: datos de prueba (semilla) y README de la entrega

**Rúbrica que cubre:** «Datos de prueba y persistencia» y «README con instrucciones reproducibles».

**Parte 1: semilla.** Archivos: `apps/api/src/P11.Api/Datos/SemillaDatos.cs` y `apps/api/src/P11.Api/appsettings.Development.json`.
- En `appsettings.Development.json`, sección `Semilla:Usuarios`, agregar dos usuarios con el mismo formato que los que ya están:
  - `productor@p11.local` (rol Productor);
  - `inversionista@p11.local` (rol Inversionista).
- En `SemillaDatos.cs`, después de crear los usuarios: **si la tabla `Proyectos` está vacía**, crear 2 proyectos para el productor de la semilla:
  - «Engorde Chiquitania 2026» (Engorde, San Ignacio de Velasco);
  - «Tambo Warnes» (Lecheria, Warnes).
  Usá `SelectorCreadorProyecto`, igual que el controlador, así los datos pasan por las mismas validaciones. Para que tenga verificación de identidad aprobada, el productor de la semilla queda con `EstadoVerificacion = Verificado`.
  Los proyectos se crean **solo si existe el productor de la semilla**. Las pruebas automáticas usan otra semilla sin productor, y así no se rompen.
- **Listo cuando:** al borrar la base (`dotnet tool restore` una vez y después `dotnet ef database drop`) y arrancar la API, `GET /api/proyectos` como admin devuelve los 2 proyectos; al reiniciar la API, siguen estando y no se duplican.

**Parte 2: README.** Archivo: `apps/api/README.md`. Reescribirlo para la entrega con:
- nombre del proyecto, versión de .NET (10) y enlace al repositorio;
- integrantes con su usuario de GitHub: Julio César Cruz Kübber (`Junior02000XD`), Sergio Ricardo Aguilera Pantoja, Jael Brandon Quiroga Vedia, Matías Raldes León (los usuarios se completan cuando cada uno los pase);
- pasos para configurar SQL Server: los de la sección «Antes de empezar», con la cadena de ejemplo **sin clave real**;
- aplicar migraciones: `dotnet ef database update` desde `apps/api/src/P11.Api` (o simplemente arrancar la API, que migra sola);
- datos de prueba: qué usuarios y proyectos crea la semilla, con sus contraseñas de desarrollo;
- abrir Swagger (`https://localhost:7080/swagger`) y cómo usar **Authorize**;
- cómo correr las pruebas (`dotnet test`).

**Listo cuando:** otra persona puede seguir el README desde cero y llegar a Swagger con datos.

---

## Qué queda del lado de Julio

- Subir la base nueva (commit y push) **antes** de que el equipo empiece.
- Invitar a los tres como colaboradores.
- Unir los pull requests con **«Create a merge commit»**, para que cada commit quede con su autor.
- Generar el PDF de evidencias, con capturas de Swagger y las pruebas 200/201/204/400/404/409 más el GET después de reiniciar, y el ZIP sin `bin`, `obj` ni `.vs`.
