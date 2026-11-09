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

## Pendiente
Análisis de los patrones **de comportamiento** (consigna del 02/11).
