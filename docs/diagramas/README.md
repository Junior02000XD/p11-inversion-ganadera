# Diagramas UML

Hay nueve diagramas, más el Modelo de Negocio que está en [`../analisis/`](../analisis/). Se editan con StarUML (`.mdj`). Para mirarlos alcanza con los PNG de [`figuras/`](figuras/), una carpeta por diagrama con un PNG por hoja.

Antes de crear o cambiar un diagrama, leé [`convenciones.md`](convenciones.md): ahí están los 12 casos de uso, los actores, el vocabulario y cómo aparece el Sistema en cada tipo de diagrama.

## Los diagramas

| # | Diagrama | Archivo | Hojas | Responsable | Relación con el código (13/11/2026) |
|---|---|---|---|---|---|
| 01 | Casos de uso (Ing. de Software) | [`uml/01-casos-de-uso.mdj`](uml/01-casos-de-uso.mdj) | 1 | Julio | 12 CU; es el alcance del producto, no cambia con el código |
| 01 | Casos de uso del sistema (Proy. de Sistemas 1) | [`v1-docente`](uml/01-casos-de-uso-sistema-v1-docente.mdj) y [`v2-justificado`](uml/01-casos-de-uso-sistema-v2-justificado.mdj) | 1 c/u | Julio | 17 CU, dos variantes; la defensa está en [`../diseno/defensa-casos-de-uso.md`](../diseno/defensa-casos-de-uso.md) |
| 02 | Actividad | [`uml/02-actividad.mdj`](uml/02-actividad.mdj) | 12 (una por CU) | Julio | CU-01 a CU-06 coinciden con lo programado (v3: en CU-06 el pago es solo QR simulado) |
| 03 | Secuencia | [`uml/03-secuencia.mdj`](uml/03-secuencia.mdj) | 13 (12 CU + reclamo al seguro) | Brandon | **Actualizado (v5)**: CU-05 (catálogo con filtros y simulador con `ConfiguracionEscenario`) y CU-06 (una sola `Inversion`: reservar, firmar con OTP, pagar con QR, vencer o rechazar), además de CU-03 y CU-04 |
| 04 | Colaboración | [`uml/04-colaboracion.mdj`](uml/04-colaboracion.mdj) | 13, misma numeración que Secuencia | Brandon | Igual que Secuencia (se derivan de los mismos mensajes) |
| 05 | Clases | [`uml/05-clases.mdj`](uml/05-clases.mdj) | 4: dominio + 3 de patrones | Sergio | **Actualizado (v7)**: el Sprint 2 como quedó en el código, una sola clase `Inversion` con `EstadoInversion` (en vez de reserva, contrato, pago y participación), `ContratoInversion` y `DatosPublicacion`; lo del Sprint 3 lleva «planificada» |
| 06 | Objetos | [`uml/06-objetos.mdj`](uml/06-objetos.mdj) | 1 | Sergio | **Actualizado (v7)**: una `Inversion` pagada (1 %, 216 kg) que une a la inversionista con el proyecto, con los datos de la demo |
| 07 | Estados | [`uml/07-estados.mdj`](uml/07-estados.mdj) | 5 (EST-1 a EST-5) | Matías | **Actualizado (v5)**: EST-4 usa los estados del código (`EstadoInversion`: Reservada, Firmada, Pagada, Vencida, Rechazada); EST-1 permite reenviar CI y selfie desde Rechazado; EST-3 marca lo implementado (State: `EstadoFicha`) |
| 08 | Componentes | [`uml/08-componentes.mdj`](uml/08-componentes.mdj) | 1 | Matías | **Actualizado**: tecnología de cada componente como estereotipo (ASP.NET Core, React + Vite, SQL Server…). Desde el 18/11 la base es SQL Server |
| 09 | Despliegue | [`uml/09-despliegue.mdj`](uml/09-despliegue.mdj) | 1 | Julio | **Actualizado**: igual que Componentes, en los artefactos (base «SQL Server» desde el 18/11) |

Mockups: [`mockups/mockups.bmpr`](mockups/mockups.bmpr) (Balsamiq Wireframes 4, 28 pantallas de celular, 12 CU). Para verlos sin Balsamiq están [`mockups/pantallas/`](mockups/pantallas/), una imagen por pantalla, y [`mockups/laminas/`](mockups/laminas/), una lámina por CU.

### Hojas del diagrama de clases

1. **Modelo de dominio.** Las clases ya programadas tienen los mismos nombres, atributos y operaciones que el código. Por ejemplo, `Usuario` es una sola clase con `roles: Rol[1..*]` (ASP.NET Identity) y no tiene subclases por rol, y `ProyectoGanadero` es abstracta, con `ProyectoEngorde` y `ProyectoLecheria`. La inversión del Sprint 2 es una sola clase, `Inversion`: la reserva, la firma del contrato, el pago y la participación son estados y campos de esa clase (`EstadoInversion`). Las clases del Sprint 3 (ronda, lote, avance, incidencia, reclamo) son diseño y llevan el estereotipo «planificada».
2. **Patrones creacionales**: Factory Method, Builder y Prototype.
3. **Patrones estructurales**: Adapter, Decorator, Proxy, Facade, Bridge y Composite.
4. **Patrones de comportamiento**: State, Command, Observer, Chain of Responsibility y Strategy.

Las hojas 2 a 4 muestran las mismas clases que [`../patrones/README.md`](../patrones/README.md) y los informes.

## Versiones con trazabilidad

[`trazabilidad/`](trazabilidad/) tiene los mismos diagramas con las etiquetas `[RF-xx]` / `[RNF-xx]` en cada elemento: sirven para revisar la cobertura de requisitos. Los de `uml/` son las versiones limpias, las que se entregan. Clases y Objetos no tienen versión con trazabilidad. **Si cambiás un diagrama, cambiá también su versión en `trazabilidad/`.**

## Cómo cambiar un diagrama

1. Abrí el `.mdj` de `uml/` en StarUML y hacé el cambio.
2. Exportá cada hoja que tocaste (*File → Export Diagram As → PNG*) y reemplazá el PNG de esa hoja en `figuras/<NN-tipo>/`, con el mismo nombre.
3. Repetí el cambio en `trazabilidad/` si el diagrama tiene esa versión.
4. Abrí un Pull Request que diga qué cambió y por qué (por ejemplo: «Secuencia CU-06: un pago rechazado deja la inversión en Rechazada»).

Los `.mdj` de clases, secuencia, colaboración, estados, componentes y despliegue se generaron con scripts que Julio tiene fuera del repositorio. Si los cambiás a mano en StarUML, **avisale en el PR**, para que el próximo cambio parta de tu versión y no la pise.
