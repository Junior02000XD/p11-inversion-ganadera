# Documentación del Proyecto 11

Todo lo que el equipo necesita además del código: requisitos, análisis, procesos, diagramas UML, diseño, patrones y la evidencia de cada sprint. Cada archivo está en su **versión vigente**. Las versiones anteriores quedan en el historial de git (`git log -- <archivo>`).

Para ver los diagramas sin instalar nada, usá los PNG de [`diagramas/figuras/`](diagramas/figuras/). Para editarlos, abrí los `.mdj` con [StarUML](https://staruml.io).

## Mapa

| Carpeta | Qué hay | Empezá por |
|---|---|---|
| [`requisitos/`](requisitos/) | ERS 2.0 (RF-01…RF-27, RNF-01…RNF-11, trazabilidad RF → CU) y la especificación ASA1 v2 de la que salió | `ers-v2.0.pdf` |
| [`analisis/`](analisis/) | PESTEL, Business Model Canvas y Modelo de Negocio (M.A actual / M.P propuesto) | `business-model-canvas.pdf` |
| [`procesos/`](procesos/) | BPMN de los cuatro procesos, AS-IS → TO-BE | `bpmn-procesos-completo.pdf` |
| [`diagramas/`](diagramas/) | Los 9 diagramas UML, sus figuras, versiones con trazabilidad y mockups | [`diagramas/README.md`](diagramas/README.md) |
| [`diseno/`](diseno/) | Documento de Diseño entregado y defensa del diagrama de casos de uso | `documento-de-diseno.pdf` |
| [`patrones/`](patrones/) | Mapa patrón → código → prueba, informes grupales y guías de las tres familias | [`patrones/README.md`](patrones/README.md) |
| [`entregas/`](entregas/) | Entregas que no son de un sprint: API Integrador de Programación Web 3 (plan, aportes del equipo, evidencias) | [`entregas/web3-api-integrador/README.md`](entregas/web3-api-integrador/README.md) |
| [`sprints/`](sprints/) | Planificación (Sprint 0) y alcance, guion de demo y capturas de cada sprint | [`sprints/README.md`](sprints/README.md) |
| [`cronograma.md`](cronograma.md) | Fechas de entrega de las dos materias | — |

## Qué se entregó y dónde está

| Fecha | Entrega | Materia | Archivos |
|---|---|---|---|
| 09/10 | PESTEL, BPMN AS-IS/TO-BE, especificación ASA1 v2 | Ing. de Software | `analisis/pestel-v2.pdf`, `procesos/`, `requisitos/especificacion-asa1-v2.pdf` |
| 20/10 | Diagramas de actividad por caso de uso | Ing. de Software | `diagramas/uml/02-actividad.mdj` |
| 21/10 | Modelo de Negocio (M.A, M.P) y Canvas | Proyecto de Sistemas 1 | `analisis/modelo-negocio-*`, `analisis/business-model-canvas*` |
| 25/10 | Sprint 0: planificación y backlog | Proyecto de Sistemas 1 | `sprints/sprint-0/` |
| 27/10 | ERS 2.0 + Documento de Diseño (9 diagramas UML + mockups) | Ing. de Software | `requisitos/ers-v2.0.*`, `diseno/documento-de-diseno.pdf`, `diagramas/` |
| 28/10 | Casos de uso del sistema (dos versiones) y su defensa | Proyecto de Sistemas 1 | `diagramas/uml/01-casos-de-uso-sistema-*`, `diseno/defensa-casos-de-uso.*` |
| 03/11, 09/11, 13/11 | Patrones creacionales, estructurales y de comportamiento | Ing. de Software | `patrones/informes/`, `patrones/guias/` |
| 06/11 | Sprint 1 (MV1) | Proyecto de Sistemas 1 | `sprints/sprint-1/` |
| 18/11 | Corrección: pestañas = casos de uso (D1), Administrador con acceso a todo | Proyecto de Sistemas 1 | `sprints/sprint-2/correccion-pestanas-casos-de-uso.md` |
| 20/11 (pendiente) | API Integrador: ZIP de la API + README + PDF de evidencias | Programación Web 3 | `entregas/web3-api-integrador/` |
| 20/11 (pendiente) | Sprint 2 (MV2) | Proyecto de Sistemas 1 | `sprints/sprint-2/` |

## Reglas para mantenerla

- **Nombres**: minúsculas con guiones, sin fecha, sin materia y sin autor. La fecha la da git y el autor está en las tablas de los README.
- **Una sola versión por archivo.** Para cambiar un documento se reemplaza el archivo con el mismo nombre en un Pull Request, que describe qué cambió. No se crean `-v2`, `-final` ni copias.
- Si cambiás un diagrama, exportá también su PNG a `diagramas/figuras/` con el mismo nombre (ver [`diagramas/README.md`](diagramas/README.md)).
- Los documentos entregados (`documento-de-diseno.pdf`, los informes de patrones, el Sprint 0) quedan como se entregaron. Si el proyecto cambia después, se actualizan los diagramas, no esos PDF.
