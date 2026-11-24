# Guía de demo en vivo: patrones creacionales

Proyecto 11, Inversión Ganadera · Equipo Los Cesaritos
Documento base: [`../informes/creacionales-informe.pdf`](../informes/creacionales-informe.pdf) (y la versión [con citas](../informes/creacionales-informe-con-citas.pdf))

Esta guía dice qué mostrar, en qué orden y qué decir de cada patrón. El análisis de patrones creacionales se hizo antes de que hubiera código; los patrones se implementaron después, en el Sprint 1. Por eso esta demo muestra el código actual del repositorio.

## Resumen: qué se muestra y cómo

| Patrón | Prioridad (§9 del análisis) | Cómo se muestra | Archivo para abrir (en `apps/api/`) |
|---|---|---|---|
| Factory Method | Media | En vivo (web) + prueba | `Models/Projects/CreadorProyecto.cs`, `ProyectoGanadero.cs` |
| Builder | Alta | Prueba (el endpoint llega en el Sprint 3) | `Models/Dossiers/ConstructorExpediente.cs` |
| Prototype | Alta | Prueba (el endpoint llega en el Sprint 2) | `Models/Simulation/ConfiguracionEscenario.cs` |
| Configuración inyectada (en lugar de Singleton) | Baja | En vivo (la API no arranca con una clave mala) + prueba | `Configuration/Opciones.cs`, `Program.cs`, `appsettings.json` |
| Abstract Factory | Baja, opcional | No implementado: se explica | — |

**Ojo:** las pantallas «Simulador» y «Expediente» de la web todavía muestran datos de ejemplo, no los del Builder ni del Prototype. No presentarlas como si usaran el patrón.

## 1. Preparación (antes de la clase)

Igual que en la guía de comportamiento:

```bash
docker compose up -d --wait                                   # desde la raíz del repo
cd apps/api && dotnet run --launch-profile https  # API
cd apps/web && npm run dev                                    # web (otra terminal)
```

Entrar en http://localhost:5173 como **productor** Luis Pérez (`luis.perez.208686@demo.bo` / `Productor.2026`). Dejar abiertos en el editor los archivos de la tabla.

## 2. Factory Method en vivo (unos 2 minutos)

| # | Qué se hace | Qué se ve | Qué se dice |
|---|---|---|---|
| 1 | «Mis proyectos» → «Nuevo proyecto». | El formulario de la ficha, con «Engorde» elegido. | RF-07: cada línea productiva tiene su ficha. |
| 2 | Cambiar a «Lechería» y volver a «Engorde». | Cambian los campos: vacas, litros y precio del litro contra cabezas, pesos y costos. | En la API, cada línea tiene su creador: `CreadorProyectoEngorde` y `CreadorProyectoLecheria`. |
| 3 | Llenar una ficha de engorde con **peso meta menor que el peso inicial** (por ejemplo 300 y 200) y apretar «Guardar borrador». | El campo «Peso meta» queda marcado: «El peso meta debe ser mayor al inicial.». | El producto (`ProyectoEngorde`) tiene su propia regla en `ValidarLinea()`: por eso hace falta Factory Method y no un `new` con un `if`. |
| 4 | Corregir el peso meta (por ejemplo 420) y guardar. | «Guardamos "…" como borrador.» y el proyecto aparece en «Borrador». | `CrearBorrador()` es el flujo común; `FabricarProyecto()` es el método fábrica que redefine cada creador. El controlador le pide el creador al `SelectorCreadorProyecto`. |

Ese borrador queda en la cuenta de Luis. No molesta para las otras demos, porque «Engorde Chiquitania 2026» sigue igual.

## 3. Configuración inyectada en vivo (1 minuto)

Mostrar `appsettings.json` (sección `ReglasSeguridad`: 5 intentos, 15 minutos de bloqueo) y explicar que no hay ningún `Instance` estático: cada servicio recibe su configuración como `IOptions<T>` y se valida al arrancar.

Para demostrarlo, en una terminal nueva (la API de la demo puede seguir corriendo; esta usa otro puerto):

```bash
cd apps/api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:7999 Archivos__ClaveBase64=abc dotnet run --no-launch-profile
```

Se ve: `OptionsValidationException: Archivos:ClaveBase64 debe ser una clave AES-256 en base64 (32 bytes).` y la API **no arranca**. Con un Singleton, el error aparecería recién cuando alguien subiera un documento.

## 4. Lo que se muestra con pruebas

Desde la raíz del repo:

```bash
dotnet test tests/api --filter "FullyQualifiedName~FactoryMethodTests"
dotnet test tests/api --filter "FullyQualifiedName~BuilderTests"
dotnet test tests/api --filter "FullyQualifiedName~PrototypeTests"
dotnet test tests/api --filter "FullyQualifiedName~ConfiguracionInyectadaTests"
```

Qué decir, abriendo las pruebas en `tests/api/Patterns/`:

- **Builder (RF-22, expediente).** `Ordena_la_cronologia_por_fecha`: los eventos se agregan en cualquier orden y el expediente sale ordenado. `Version_de_cierre_exige_venta_y_liquidacion`: un expediente de cierre sin venta no se construye. `Dos_construcciones_no_comparten_estado_y_un_constructor_no_se_reutiliza`: un constructor por solicitud.
- **Prototype (RF-12, simulador).** `Modificar_la_copia_no_altera_la_base_ni_otras_copias`: las variantes pesimista y optimista se sacan clonando la base. `La_copia_no_hereda_el_resultado_anterior`. `Prepara_tres_variantes_ordenadas_y_con_advertencia`: cada resultado lleva «Estimación, no rentabilidad garantizada» (RNF-06). La fórmula es provisional hasta definirla con el cliente.
- **Factory Method.** `Lecheria_sin_sus_campos_no_se_crea_y_marca_cada_campo` y `Un_creador_no_acepta_datos_de_otra_linea`.
- **Configuración inyectada.** `Una_configuracion_invalida_se_rechaza` y `Se_puede_sustituir_sin_estado_global`: las pruebas cambian la configuración sin tocar nada global.

## 5. Abstract Factory (sin demo)

No se implementó. El análisis (§8.2) pide implementarlo solo si se confirma que las familias de validadores (RF-18/19) varían juntas por línea productiva, y eso todavía no está confirmado. Decirlo así: «se analizó y se dejó para cuando haya evidencia».

## 6. Si algo falla en vivo

Si la web no responde, correr las pruebas de la sección 4. Hacen lo mismo que la demo y no dependen de Docker.
