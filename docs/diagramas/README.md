# Diagramas UML

Hay nueve diagramas, más el Modelo de Negocio que está en [`../analisis/`](../analisis/). Se editan con StarUML (`.mdj`). Para mirarlos alcanza con los PNG de [`figuras/`](figuras/), una carpeta por diagrama con un PNG por hoja.

Antes de crear o cambiar un diagrama, leé [`convenciones.md`](convenciones.md): ahí están los 12 casos de uso, los actores, el vocabulario y cómo aparece el Sistema en cada tipo de diagrama.

## Los diagramas

| # | Diagrama | Archivo | Hojas | Responsable | Relación con el código (13/11/2026) |
|---|---|---|---|---|---|
| 01 | Casos de uso (Ing. de Software) | [`uml/01-casos-de-uso.mdj`](uml/01-casos-de-uso.mdj) | 1 | Julio | 12 CU; es el alcance del producto, no cambia con el código |
| 01 | Casos de uso del sistema (Proy. de Sistemas 1) | [`v1-docente`](uml/01-casos-de-uso-sistema-v1-docente.mdj) y [`v2-justificado`](uml/01-casos-de-uso-sistema-v2-justificado.mdj) | 1 c/u | Julio | 17 CU, dos variantes; la defensa está en [`../diseno/defensa-casos-de-uso.md`](../diseno/defensa-casos-de-uso.md) |
| 02 | Actividad | [`uml/02-actividad.mdj`](uml/02-actividad.mdj) | 12 (una por CU) | Julio | CU-01 a CU-04 coinciden con lo programado |
| 03 | Secuencia | [`uml/03-secuencia.mdj`](uml/03-secuencia.mdj) | 13 (12 CU + reclamo al seguro) | Brandon | **Actualizado**: CU-03 (borrador → enviar a revisión con requisitos) y CU-04 (observar, rechazar o aprobar y publicar) |
| 04 | Colaboración | [`uml/04-colaboracion.mdj`](uml/04-colaboracion.mdj) | 13, misma numeración que Secuencia | Brandon | Igual que Secuencia (se derivan de los mismos mensajes) |
| 05 | Clases | [`uml/05-clases.mdj`](uml/05-clases.mdj) | 4: dominio + 3 de patrones | Sergio | **Rehecho**: las clases programadas son las de `apps/api/Dominio`; las de los Sprints 2-3 llevan «planificada» |
| 06 | Objetos | [`uml/06-objetos.mdj`](uml/06-objetos.mdj) | 1 | Sergio | **Actualizado**: `Usuario` con rol y `ProyectoEngorde`, con los datos de la demo |
| 07 | Estados | [`uml/07-estados.mdj`](uml/07-estados.mdj) | 5 (EST-1 a EST-5) | Matías | **Actualizado**: EST-1 permite reenviar CI y selfie desde Rechazado; EST-3 marca lo implementado (State: `EstadoFicha`) |
| 08 | Componentes | [`uml/08-componentes.mdj`](uml/08-componentes.mdj) | 1 | Matías | **Actualizado**: tecnología de cada componente como estereotipo (ASP.NET Core, React + Vite, SQL Server…). Desde el 18/11 la base es SQL Server |
| 09 | Despliegue | [`uml/09-despliegue.mdj`](uml/09-despliegue.mdj) | 1 | Julio | **Actualizado**: igual que Componentes, en los artefactos (base «SQL Server» desde el 18/11) |

Mockups: [`mockups/mockups.bmpr`](mockups/mockups.bmpr) (Balsamiq Wireframes 4, 28 pantallas de celular, 12 CU). Para verlos sin Balsamiq están [`mockups/pantallas/`](mockups/pantallas/), una imagen por pantalla, y [`mockups/laminas/`](mockups/laminas/), una lámina por CU.

### Hojas del diagrama de clases

1. **Modelo de dominio.** Las clases ya programadas tienen los mismos nombres, atributos y operaciones que el código. Por ejemplo, `Usuario` es una sola clase con `roles: Rol[1..*]` (ASP.NET Identity) y no tiene subclases por rol, y `ProyectoGanadero` es abstracta, con `ProyectoEngorde` y `ProyectoLecheria`. Las clases de los Sprints 2-3 (ronda, reserva, contrato, pago, participación, lote, avance, incidencia, reclamo) son diseño y llevan el estereotipo «planificada».
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
4. Abrí un Pull Request que diga qué cambió y por qué (por ejemplo: «Secuencia CU-06: pago rechazado vuelve a Pendiente de pago»).

Los `.mdj` de clases, secuencia, colaboración, estados, componentes y despliegue se generaron con scripts que Julio tiene fuera del repositorio. Si los cambiás a mano en StarUML, **avisale en el PR**, para que el próximo cambio parta de tu versión y no la pise.
