# Sprint 2 — MV2 (20/11/2026)

**Alcance:** evaluación y publicación de proyectos (RF-09, RF-10), catálogo (RF-11), simulador de escenarios (RF-12) e inversión. La inversión comprende el cupo y la reserva (RF-13), el contrato con OTP (RF-14), el pago QR simulado (RF-15) y la asignación de la participación (RF-16). La regla del sprint es la del Sprint 1: cada RF tiene endpoint, pantalla y prueba.

La explicación completa, con capturas, flujo y reglas de cada casilla, está en la [guía de estudio](../../guia/guia-de-estudio.html) (sección 7).

## Requerimientos → implementación → prueba

| Req. | Criterio de aceptación (ERS) | API | Pantalla web | Prueba automática |
|---|---|---|---|---|
| RF-09 | Observado: vuelve al productor con observaciones. Rechazado: guarda el motivo y no se publica. | `GET /api/analista/proyectos`, `POST /api/analista/proyectos/{id}/decision` | `/evaluacion`, `/evaluacion/:id` | `EvaluacionProyectosTests`, `CommandTests`, `ObserverTests` |
| RF-10 | No se publica ningún proyecto sin riesgos y tres escenarios cargados. | La decisión «Aprobar» exige nivel de riesgo, riesgos y los supuestos de los escenarios (`DatosPublicacion`) | `/evaluacion/:id` | `EvaluacionProyectosTests`, `CommandTests`, `StateTests` |
| RF-11 | Al filtrar solo aparecen los proyectos que cumplen el filtro. | `GET /api/catalogo?linea=&plazoMaximo=&minimoHasta=&zona=&riesgo=`, `GET /api/catalogo/{id}` | `/catalogo` | `CatalogoTests` |
| RF-12 | Se muestran tres resultados y la advertencia de que no son garantizados. | `POST /api/catalogo/{id}/simulacion` (Prototype) | `/catalogo/:id` | `CatalogoTests`, `PrototypeTests` |
| RF-13 | Sin cupo o bajo el mínimo: se rechaza mostrando el cupo disponible. Válido: se reserva y vence a los 30 min. | `POST /api/inversiones` (400 bajo el mínimo, 409 sin cupo) | `/inversiones`, `/inversiones/nueva/:proyectoId` | `InversionesTests` |
| RF-14 | Sin aceptación no se habilita el pago; la evidencia no puede borrarse. | `GET /api/inversiones/{id}/contrato`, `POST …/contrato/codigo`, `POST …/contrato/firmar` | `/inversiones/:id/contrato` | `InversionesTests` |
| RF-15 | Confirmado: sigue la asignación. Rechazado o vencido: cupo liberado y aviso enviado. | `POST /api/inversiones/{id}/pago`, `POST …/pago/confirmar`; servicio `VencimientoReservas` | `/inversiones/:id/pago` | `InversionesTests` |
| RF-16 | Tras un pago confirmado la participación aparece en el portafolio con su constancia. | `GET /api/portafolio`, `GET /api/portafolio/{id}/constancia` | `/portafolio`, `/portafolio/:id` | `InversionesTests` |

Resultado: `dotnet test` → **112 pruebas, 0 fallas**: 87 antes del sprint, 13 del CRUD (Sergio, Web 3) y 12 del Sprint 2. `npm run build` sin errores.

## Decisiones tomadas en el sprint

- **Reserva de 30 minutos**, como dice la ERS (el plan del 18/11 decía 15). Una reserva vencida deja de ocupar cupo en el acto. El servicio de fondo `VencimientoReservas` la marca como «Vencida» cada minuto y avisa por correo.
- **RF-10 con datos reales.** La casilla provisional del checklist se reemplazó por el nivel de riesgo, el texto de riesgos y cuatro supuestos: rendimiento, costos, mortalidad y variación entre escenarios. Con ellos el simulador arma los tres escenarios.
- **Fórmula provisional del simulador** (la definitiva es con el cliente, ERS RF-12): valor = monto × (1 + rendimiento − costos − mortalidad). El pesimista resta la variación al rendimiento y el optimista la suma.
- **Pago QR simulado.** La API tiene los dos momentos de una pasarela real: generar el cobro y recibir la confirmación. El botón «Ya pagué, verificar» hace de pasarela, y «Simular pago rechazado» muestra el camino del rechazo.
- **Participación**: % = aporte ÷ meta × 100; en engorde, además, kg = % × cabezas × peso inicial. La constancia lleva el código `CP-AAAAMMDD-XXXXXX`.
- **La ronda queda abierta 30 días** desde la publicación. Su cierre (RF-17) es del Sprint 3.
- Una sola reserva en curso por inversionista y proyecto.
- Montos en formato boliviano (`Bs 1.000,00`) en contratos y avisos, y mensajes de Identity y de la ficha en español.

## Guion de la demo (≈ 8 min)

Requisitos: SQL Server, `docker compose up -d` (Mailpit), la API y la web (ver el [`README.md`](../../../README.md) de la raíz).

1. **Productor** verificado y con documentos aprobados envía «Engorde Chiquitania 2026» a evaluación (como en el Sprint 1).
2. **Analista** (`analista@p11.local`): «Evaluar». Aprobar sin la variación → 409, no publica. Completar riesgos y supuestos → publicado. Mostrar el correo al productor en Mailpit.
3. **Inversionista**: catálogo, filtrar por Engorde, abrir el proyecto y simular Bs 1.000 → 1.040 / 1.120 / 1.200 con la advertencia.
4. «Invertir en este proyecto»: Bs 50 → error con el cupo disponible. Reservar Bs 1.000.
5. Contrato: pedir el código, leerlo en Mailpit, firmar.
6. Pago QR: «Ya pagué, verificar» → constancia (1 %, 216 kg). Mostrar el portafolio y el correo de la constancia.
7. **Administrador**: «Ver como» en un CU con varios actores y la bitácora.
8. `dotnet test` en verde.

## Capturas

Las 33 capturas del recorrido completo (Sprints 1 y 2, con la API y SQL Server reales) están en [`../../guia/capturas/`](../../guia/capturas/), numeradas en el orden de la demo. Las de la corrección del 18/11 siguen en [`capturas/`](capturas/).

## Fuera del Sprint 2

- Cierre de la ronda (RF-17), compra, traslado, avance, incidencias, expediente y liquidación: Sprint 3. Esas pantallas siguen con `datosEjemplo.js`.
- Cliente Android: Sprint 3.

También se hizo en este sprint, fuera de los RF: la [corrección del ingeniero](correccion-pestanas-casos-de-uso.md) (pestañas = casos de uso, Administrador con acceso a todo) y el CRUD de proyectos de Programación Web 3.
