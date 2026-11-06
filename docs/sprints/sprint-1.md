# Sprint 1 — MV1 (06/11/2026)

Alcance: registro con OTP, inicio de sesión con bloqueo, verificación de identidad simulada, roles, documentos del predio y ficha de proyecto en borrador. Requerimientos de la E.R.S. v2.0.

## Requerimientos → implementación → prueba

| Req. | Criterio de aceptación (ERS) | API | Pantalla web | Prueba automática |
|---|---|---|---|---|
| RF-01 | Con datos válidos se crea la cuenta y se envía un OTP. Con correo o CI ya registrado: error, no se crea. | `POST /api/auth/registro` | `/registro` | `AutenticacionTests.RF01_*` |
| RNF-01 | Sin el código correcto la operación no se completa. | `POST /api/auth/otp/verificar`, `/otp/reenviar` | `/verificar-cuenta` | `AutenticacionTests.RNF01_*`, `OtpServiceTests` (vencido, 3 intentos, un solo uso, solo hash) |
| RF-02 | Correctas: abre sesión. Incorrectas: error y suma un intento. Al 5.º: bloqueo de 15 min y aviso por correo. | `POST /api/auth/login` (401 con intentos restantes, 423 bloqueada) | `/ingresar` | `AutenticacionTests.RF02_*` |
| RF-03 | Imágenes ilegibles: se piden nuevas. Coincidencia menor al umbral: revisión manual. | `POST /api/verificacion` (proveedor simulado) | `/identidad` | `VerificacionTests.RF03_*` |
| RF-04 | Un usuario no verificado no puede publicar. Cada cambio guarda fecha, responsable y motivo. | `GET /api/verificacion/estado`, `POST /api/analista/verificaciones/{id}/decision` | `/identidad`, `/analista` | `VerificacionTests.RF03_RF04_*`, `ProyectosTests.RF04_*` |
| RF-05 | Sin documentos aprobados el proyecto no pasa a revisión técnica. | `POST /api/productor/documentos`, `POST /api/analista/documentos/{id}/decision`, `POST /api/proyectos/{id}/enviar-revision` | `/documentos`, `/analista`, `/proyectos` | `ProyectosTests.RF05_*` |
| RF-06 | Un inversionista no puede publicar; cada rol accede solo a lo suyo. | `[Authorize(Roles=…)]`, `GET/PUT /api/admin/usuarios` | `/admin/usuarios`, inicio según rol | `RolesTests` |
| RF-07 | Al elegir la línea solo se muestran sus campos; la ficha se guarda como borrador. | `POST /api/proyectos` (Factory Method) | `/proyectos/nuevo` | `ProyectosTests.RF07_*`, `FactoryMethodTests` |
| RNF-02 | Tráfico cifrado (TLS); los archivos de CI no se abren sin la clave del sistema. | HTTPS + AES-256-GCM en `AlmacenCifrado` | — | `VerificacionTests.RNF02_*` |
| RNF-07 | Sin consentimiento no se puede cargar el CI ni la selfie. | `POST /api/verificacion` exige `consentimiento=true` | casilla en `/identidad` | `VerificacionTests.RNF07_*` |

Resultado: `dotnet test` → **44 pruebas, 0 fallas**.

## Decisiones tomadas en el sprint
- **Proveedor de verificación de identidad simulado** detrás de `IVerificadorIdentidad` (no hay convenio). Devuelve 92 % de coincidencia contra un umbral de 80 %. Una selfie cuyo nombre de archivo contiene `revision` devuelve un puntaje bajo, para mostrar el camino de revisión manual.
- **OTP por correo** (ERS v2.0), de 6 dígitos, vigente 10 minutos y con 3 intentos. En desarrollo los correos llegan a **Mailpit** (http://localhost:8025), sin envío real.
- **Bloqueo** con ASP.NET Core Identity (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min`), parametrizado en `ReglasSeguridad` del `appsettings.json`. El aviso por correo se envía una sola vez por bloqueo.
- **Ficha en borrador**: ya valida los campos de su línea (adelanta parte de RF-08). Pasar a revisión exige identidad verificada (RF-04) y RUNSA más título o contrato aprobados (RF-05).
- **Roles internos** (Analista y Administrador) no se autoregistran. Se crean con la semilla de `appsettings.Development.json` al arrancar la API.

## Guion de la demo (≈ 8 min)
Requisitos (ver `README.md`): `docker compose up -d`, la API (`dotnet run --launch-profile https` en `apps/api/src/P11.Api`) y el web (`npm run dev` en `apps/web`). Abrir http://localhost:5173 y Mailpit en http://localhost:8025.

1. **Registro (RF-01)**: crear un productor, mostrar el OTP en Mailpit y confirmarlo. Intentar registrar el mismo CI otra vez: error.
2. **Login (RF-02)**: dos contraseñas incorrectas («Quedan 3 intentos») y después la correcta. El inicio muestra el rol y la identidad «Pendiente».
3. **Identidad (RF-03/04, RNF-07, RNF-02)**: el botón no se habilita sin la casilla de consentimiento. Subir CI y selfie → «Verificado», con historial (fecha, responsable «sistema», motivo).
4. **Ficha (RF-07)**: cambiar entre Engorde y Lechería y ver que cambian los campos. Guardar incompleta marca cada campo; completa queda como **Borrador**.
5. **Enviar a evaluación** sin documentos → «Faltan documentos aprobados» (RF-05). Subir RUNSA y contrato.
6. **Analista**: entrar con `analista@p11.local` y aprobar los documentos. Mostrar también una identidad en revisión manual (otro usuario con selfie `…revision…`): «Ver selfie» la abre descifrada.
7. **Productor**: «Enviar a evaluación técnica» → **En revisión**.
8. **Bloqueo**: 5 intentos fallidos → cuenta bloqueada con la hora de desbloqueo, y el correo de aviso en Mailpit.
9. **Administrador (RF-06)**: entrar con `admin@p11.local`, buscar un usuario, cambiar su rol y desbloquear la cuenta del paso 8.

Capturas de este recorrido: `…/04_Contexto_Proyecto11/05_Entregas/03_Sprint1_MV1/20261105_PROYSIST1_Sprint1_Capturas-Demo/`.

## Fuera del Sprint 1
- Cliente Android (Java): se inicia con el Sprint 3 (RNF-04, captura sin conexión).
- Bitácora inmodificable (RNF-03), catálogo, simulador y pagos: Sprint 2 en adelante. El Prototype del simulador y el Builder del expediente ya existen en el dominio, con pruebas.
