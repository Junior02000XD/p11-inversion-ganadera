# Patrones de diseño (Ingeniería de Software)

Implementación de los patrones creacionales elegidos en el análisis grupal (§9, «Selección de patrones creacionales»). Cada fila enlaza el patrón con su código, el requerimiento que lo motiva y la prueba que lo demuestra. Rutas relativas a `apps/api/`.

| Patrón | Prioridad (§9) | Dónde | RF | Prueba | Estado |
|---|---|---|---|---|---|
| Factory Method | Media | `src/P11.Api/Dominio/Proyectos/CreadorProyecto.cs` (creador), `ProyectoGanadero.cs` (productos), `SelectorCreadorProyecto`, registro en `Program.cs` | RF-07 | `tests/P11.Tests/Patrones/FactoryMethodTests.cs`, `Integracion/ProyectosTests.cs` | En uso (`POST /api/proyectos`) |
| Builder | Alta | `src/P11.Api/Dominio/Expediente/ConstructorExpediente.cs` | RF-22 | `tests/P11.Tests/Patrones/BuilderTests.cs` | Dominio y pruebas; el endpoint llega en el Sprint 3 |
| Prototype | Alta | `src/P11.Api/Dominio/Simulacion/ConfiguracionEscenario.cs` | RF-12 | `tests/P11.Tests/Patrones/PrototypeTests.cs` | Dominio y pruebas; el endpoint llega en el Sprint 2 |
| Configuración inyectada (en lugar de Singleton) | Baja | `src/P11.Api/Configuracion/Opciones.cs` + `AddOptions<…>().ValidateOnStart()` en `Program.cs` | RF-02, RNF-01, RNF-02 | `tests/P11.Tests/Patrones/ConfiguracionInyectadaTests.cs` | En uso |
| Abstract Factory | Baja (opcional) | — | RF-18/19 | — | No se implementa hasta validar que las familias de validadores varían juntas (§8.2) |

## Cómo se aplica cada uno

**Factory Method (RF-07).**
- `CreadorProyecto.CrearBorrador()` es el flujo común: asigna los datos generales, valida y deja la ficha en Borrador.
- `FabricarProyecto()` es el método fábrica, y cada línea lo redefine: `CreadorProyectoEngorde` produce un `ProyectoEngorde` con animales y pesos; `CreadorProyectoLecheria`, un `ProyectoLecheria` con vacas, litros y precio.
- Se justifica porque los productos tienen **conducta propia**: cada uno valida sus invariantes en `ValidarLinea()`, por ejemplo «peso meta mayor al inicial». Es la condición que el análisis puso en §8.1 para no quedarse en una fábrica simple.
- El controlador no decide qué clase instanciar: le pide el creador al `SelectorCreadorProyecto`, que recibe los creadores por inyección de dependencias.

**Builder (RF-22).**
- `IConstructorExpediente` ofrece los pasos (`ConFicha`, `AgregarDocumento`, `AgregarEvento`, `ComoVersionDeCierre`) y `Construir()` entrega una `VistaExpediente` inmutable con la cronología ordenada.
- `Construir()` verifica que todo pertenezca al mismo proyecto y que una versión de cierre tenga venta y liquidación.
- Se usa un constructor por solicitud; reutilizarlo después de `Construir()` lanza una excepción. No persiste nada: la historia la guardan los servicios y la bitácora (RNF-03).

**Prototype (RF-12).** `ConfiguracionEscenario.Clonar()` aplica la política de copia del §8.4:
- duplica las colecciones modificables (supuestos y riesgos);
- copia los valores inmutables;
- da un `IdSimulacion` nuevo y descarta el resultado anterior.

`PreparadorEscenarios.Preparar()` obtiene las variantes pesimista, esperada y optimista sin tocar la base. Cada resultado lleva la advertencia «Estimación, no rentabilidad garantizada» (RNF-06). La fórmula es provisional hasta definirla con el cliente.

**Configuración inyectada.** No hay `Instance` estática. Cada sección del `appsettings.json` (`ReglasSeguridad`, `Otp`, `Archivos`, `Jwt`, `Correo`, `Verificacion`) se valida al arrancar: una clave de cifrado inválida impide iniciar la API. Los servicios la reciben como `IOptions<T>` explícito, y las pruebas la sustituyen sin estado global (§8.5).

# Patrones estructurales

Dinámica grupal del 09/11. De los siete patrones del catálogo GoF, seis quedaron en el código con sus pruebas. Flyweight no se implementa: el análisis concluyó que no hace falta (ver abajo).

| Patrón | Dónde | RF | Prueba | Estado |
|---|---|---|---|---|
| Adapter | `src/P11.Api/Servicios/CorreoSmtpService.cs` | RF-01, RF-02, RF-04 | `tests/P11.Tests/Patrones/AdapterTests.cs` | En uso (OTP y avisos) |
| Decorator | `Servicios/AlmacenDisco.cs` + `AlmacenCifrado.cs`; `CorreoConReintentos.cs`; composición en `Program.cs` | RNF-02, RF-01 | `Patrones/DecoratorTests.cs`, `Integracion/VerificacionTests.cs` | En uso |
| Proxy (de protección) | `Servicios/Documentos/VisorDocumentos.cs` + `VisorDocumentosProxy.cs` | RNF-02, RF-05 | `Patrones/ProxyTests.cs` | En uso (`GET /api/analista/archivos/{id}`, `GET /api/productor/documentos/{id}/archivo`) |
| Facade | `Servicios/VerificacionService.cs` | RF-03, RF-04 | `Integracion/VerificacionTests.cs` | En uso (`VerificacionController`, `AnalistaController`) |
| Bridge | `Servicios/Avisos/` (`Aviso`, `AvisoVerificacion`, `AvisoBloqueo` × `ICanalAviso`, `CanalCorreo`, `CanalTelegram`) | RF-02, RF-04, RF-20 | `Patrones/BridgeTests.cs` | En uso por correo; Telegram listo y probado, sin usuarios vinculados |
| Composite | `src/P11.Api/Dominio/Liquidaciones/` | RF-23 | `Patrones/CompositeTests.cs` | Dominio y pruebas; el endpoint llega en el Sprint 3 |
| Flyweight | — | RF-18 | — | No aplica |

## Cómo se aplica cada uno

**Adapter.** El sistema solo conoce `ICorreoService.EnviarAsync(para, asunto, cuerpo)`. `CorreoSmtpService` traduce esa llamada a `SmtpClient`, `MailMessage` y las credenciales de .NET. Ni `OtpService` ni los avisos saben que existe SMTP. Para pasar a MailKit, que es lo que Microsoft recomienda en lugar de `SmtpClient`, basta otro adaptador. La pasarela QR (RF-15) y el proveedor de identidad (RF-03) seguirán la misma forma.

**Decorator.**
- `AlmacenCifrado` envuelve a cualquier `IAlmacenArchivos`: cifra con AES-256-GCM antes de pasarle los bytes y descifra al leer. `AlmacenDisco` solo escribe en la carpeta.
- El formato en disco no cambió (nonce · tag · cifrado), así que los archivos que ya estaban se siguen leyendo; hay una prueba que lo comprueba.
- `CorreoConReintentos` envuelve al SMTP. Ante un corte breve, el OTP del registro ya no se pierde.

**Proxy.** `VisorDocumentosProxy` tiene la misma interfaz que el visor real y se pone delante de él:
- deja abrir un documento solo a su dueño o a un analista;
- registra cada acceso;
- a cualquier otro le responde 404, como si el documento no existiera.

La regla depende del documento y no solo del rol, por eso `[Authorize(Roles = …)]` no alcanza. Con RF-22 se sumará el inversionista de los proyectos donde invirtió.

**Facade.** `VerificacionService` ofrece dos operaciones, `EnviarAsync` y `DecidirAsync`. Esconde el almacén cifrado, el proveedor de identidad, el umbral, el historial, el guardado y el aviso. Antes, los dos controladores repetían la secuencia «cambiar estado → guardar → notificar».

**Bridge.** `Aviso` define qué se dice (asunto, cuerpo y si es urgente) e `ICanalAviso` define por dónde llega. Un aviso nuevo (`AvisoIncidencia`, RF-20) sirve para todos los canales, y un canal nuevo (push en Android, Sprint 3) sirve para todos los avisos. `CanalTelegram` no está registrado en `Program.cs` porque los usuarios todavía no tienen un chat vinculado. El OTP sigue yendo solo por correo, porque su función es comprobar que el correo es del usuario.

**Composite.** `ConceptoLiquidacion` puede ser un `ConceptoSimple` o un `GrupoConceptos`; por ejemplo, Costos contiene Sanidad, que contiene Vacunas y Veterinario. `Liquidacion` calcula venta − costos − pérdidas − comisión y arma el detalle sin distinguir entre uno y otro. El reparto proporcional al aporte es provisional hasta acordarlo con el cliente.

**Flyweight (no aplica).** Compartir los datos de la raza entre los animales de un lote ahorraría unos 100 bytes por animal: de 30 KB a 1 MB en los casos realistas. La base de datos ya guarda la raza una sola vez y el seguimiento (RF-18) se registra por lote.

# Patrones de comportamiento

Dinámica grupal del 13/11. Son los nueve patrones de la consigna. Cinco tienen código nuevo, dos ya estaban en el código (Template Method e Iterator) y dos no se aplican (Mediator y Memento). Los cuatro primeros trabajan juntos en el ciclo de vida del proyecto (RF-09, RF-10), que es lo que se muestra en la demo.

| Patrón | Dónde | RF | Prueba | Estado |
|---|---|---|---|---|
| State | `src/P11.Api/Dominio/Proyectos/Estados/EstadoFicha.cs`; operaciones en `ProyectoGanadero` | RF-08, RF-09, RF-10 (EST-3) | `Patrones/StateTests.cs` | En uso |
| Chain of Responsibility | `Servicios/Proyectos/RequisitosEnvio.cs` | RF-04, RF-05, RF-08 | `Patrones/ChainOfResponsibilityTests.cs`, `Integracion/ProyectosTests.cs` | En uso (`POST /api/proyectos/{id}/enviar-revision`) |
| Command | `Servicios/Proyectos/ComandosProyecto.cs` + `GestorCicloProyecto.cs` (invocador); bitácora `HistorialProyecto` | RF-09, RNF-03 | `Patrones/CommandTests.cs` | En uso (`POST /api/analista/proyectos/{id}/decision`) |
| Observer | `Servicios/Proyectos/ObservadoresProyecto.cs` (`AvisarAlProductor`, `AvisarALosAnalistas`); registro en `Program.cs` | RF-09 | `Patrones/ObserverTests.cs`, `Integracion/EvaluacionProyectosTests.cs` | En uso (avisos en Mailpit) |
| Strategy | `Dominio/Liquidaciones/ReglaReparto.cs` (`IReglaReparto`, `RepartoProporcional`) | RF-23 | `Patrones/StrategyTests.cs` | Dominio y pruebas; el endpoint llega en el Sprint 3 |
| Template Method | `ProyectoGanadero.Validar()` → `ValidarLinea()`; `CreadorProyecto.CrearBorrador()`; `Aviso.EnviarAsync()` | RF-07, RF-08, RF-02, RF-04 | `Patrones/TemplateMethodTests.cs` | Ya estaba en el código |
| Iterator | `GrupoConceptos.Detalle()` (`yield`), `ValidarLinea()`; `IEnumerable`/LINQ | RF-23 | `Patrones/IteratorTests.cs` | Lo da C#; no se escribe un iterador a mano |
| Mediator | — | RF-13 a RF-16 | — | No aplica |
| Memento | — | RF-07, RF-12 | — | No aplica |

## Cómo se aplica cada uno

**State (EST-3).**
- Antes, el estado era solo un enum y la regla «solo un borrador se envía» estaba en el controlador.
- Ahora cada estado es una clase (`EnBorrador`, `EnRevisionTecnica`, `Rechazado`, `EnFinanciamiento`) que dice qué operaciones admite y a cuál pasa. Lo que no admite lanza `TransicionInvalidaException` y la API responde 409 con el motivo, por ejemplo «Un proyecto en financiamiento no se puede rechazar.».
- La base sigue guardando el enum; `EstadoFicha.De(valor)` devuelve el objeto. `Estado` tiene `private set`: solo cambia con `EnviarARevision`, `Observar`, `Rechazar` y `AprobarYPublicar`.
- El resto del ciclo (cierre de ronda, compra, ejecución, venta, liquidación) se suma como clases nuevas en los Sprints 2 y 3. La verificación de identidad (EST-1) no se pasó a State: tiene cuatro estados y dos decisiones, y el chequeo cabe en una línea.

**Chain of Responsibility.** Para enviar la ficha a evaluación hay tres requisitos: ficha completa (RF-08), productor verificado (RF-04) y documentos del predio aprobados (RF-05). Cada uno es un eslabón; el primero que no se cumple responde y corta la cadena. Los mensajes son los mismos de antes. La cadena se arma en un solo lugar (`RequisitosEnvio`), de lo más barato a lo más caro. Encaja también en RF-13 (cupo, mínimo, verificación antes de reservar) y RF-19 (dato fuera de rango que pasa al analista).

**Command.** Cada acción sobre el proyecto es un objeto (`EnviarARevision`, `ObservarProyecto`, `RechazarProyecto`, `AprobarYPublicarProyecto`) con quién la pide y por qué. `GestorCicloProyecto` las ejecuta todas igual: aplica la transición, guarda una fila en `HistorialProyecto` (RNF-03) y avisa a los observadores. No hay «deshacer»: lo decidido es evidencia. El uso más fuerte llega con Android (Sprint 3): las acciones hechas sin conexión se guardan como comandos y se envían al reconectar (RNF-04).

**Observer.** `GestorCicloProyecto` no sabe quién se entera de los cambios: recibe la lista de `IObservadorProyecto` del contenedor. `AvisarAlProductor` le manda la decisión (con las observaciones) y `AvisarALosAnalistas` avisa cuando entra un proyecto a revisión. Si un aviso falla, se registra y la decisión no se revierte. RF-20 (incidencia → todos los inversionistas del proyecto) será otro observador.

**Strategy.** El ERS dice que el reparto de utilidades no está definido y debe salir de un modelo financiero validado. `Liquidacion` recibe una `IReglaReparto`; hoy la única es `RepartoProporcional` (lo que ya calculaba). La regla que defina el cliente será otra clase. `IVerificadorIdentidad` ya funcionaba igual: `VerificacionService` usa el simulado y el proveedor real lo reemplazará.

**Template Method (ya estaba).** `ProyectoGanadero.Validar()` aplica las reglas comunes y llama a `ValidarLinea()`, que redefine cada línea. `CreadorProyecto.CrearBorrador()` es el método plantilla que llama al Factory Method. `Aviso.EnviarAsync()` arma el mensaje y cada aviso pone su `Asunto` y su `Cuerpo`.

**Iterator (ya estaba).** C# lo trae con `IEnumerable` y `yield`. `GrupoConceptos.Detalle()` recorre el árbol del Composite en preorden sin mostrar cómo guarda sus hijos, y cada recorrido es independiente. El catálogo paginado (RF-11) lo resolverá EF con `Skip`/`Take`.

**Mediator (no aplica).** El candidato era el flujo de inversión (reserva, contrato, pago, participación: RF-13 a RF-16). Son pasos en secuencia y no objetos que conversan entre sí; alcanza con una Facade (como `VerificacionService`) y con Observer para los avisos. Una biblioteca como MediatR agregaría una indirección más sin resolver nada.

**Memento (no aplica).** No hay requisito de deshacer. El simulador (RF-12) ya conserva la configuración base gracias a Prototype, y la historia del proyecto se guarda en la base (`HistorialProyecto`, RNF-03), no en instantáneas en memoria. Sería útil si el cliente pide comparar la ficha observada con la corregida.
