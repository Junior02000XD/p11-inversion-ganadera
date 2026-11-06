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

## Pendiente
Hacer el mismo análisis para los patrones **estructurales** y **de comportamiento** (consigna del 02/11), después del Sprint 1.
