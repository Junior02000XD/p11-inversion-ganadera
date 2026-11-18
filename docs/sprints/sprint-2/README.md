# Sprint 2 — MV2 (20/11/2026)

**Alcance:** evaluación y publicación de proyectos (RF-09, RF-10), catálogo (RF-11), simulador de escenarios (RF-12) e inversión. La inversión comprende el cupo y la reserva (RF-13), el contrato con OTP (RF-14), el pago QR simulado (RF-15) y la asignación de la participación (RF-16). La regla del sprint es la del Sprint 1: cada RF tiene endpoint, pantalla y prueba.

Plan escrito el 18/11/2026 para la sesión siguiente. Cuando el sprint cierre, este archivo tiene que quedar igual que [`../sprint-1/README.md`](../sprint-1/README.md): tabla requerimiento → endpoint → pantalla → prueba, guion de la demo y capturas en `capturas/`.

## Estado al 18/11

| RF | Qué | API | Web | Pruebas |
|---|---|---|---|---|
| RF-09 | Evaluar proyecto (observar, rechazar, aprobar) | ✓ `api/analista/proyectos` | ✓ `Evaluacion.jsx`, `EvaluarProyecto.jsx` | ✓ `EvaluacionProyectosTests` |
| RF-10 | Publicar (riesgos + tres escenarios) | ✓ con casillas provisionales hasta el simulador | ✓ | ✓ |
| RF-11 | Catálogo con filtros | — | datos de ejemplo (`Catalogo.jsx`) | — |
| RF-12 | Simulador pesimista / esperado / optimista | dominio listo: `Dominio/Simulacion/ConfiguracionEscenario.cs` (Prototype), sin endpoint | datos de ejemplo (`Simulador.jsx`) | ✓ `PrototypeTests` (solo dominio) |
| RF-13 | Validar cupo y reservar | — | datos de ejemplo (`Invertir.jsx`) | — |
| RF-14 | Contrato y riesgos con OTP | — | datos de ejemplo (`Contrato.jsx`) | — |
| RF-15 | Pago QR simulado | — | datos de ejemplo (`PagoQr.jsx`) | — |
| RF-16 | Participación asignada | — | datos de ejemplo (`Portafolio.jsx`) | — |

También está hecho, fuera de los RF: la **corrección del ingeniero** (pestañas = casos de uso, Admin con acceso a todo) en [`correccion-pestanas-casos-de-uso.md`](correccion-pestanas-casos-de-uso.md), y el CRUD de proyectos de Programación Web 3.

## Antes de empezar: los aportes del equipo (Web 3)

Sergio, Brandon y Matías trabajan en ramas `aporte/<nombre>` (guía en [`../../entregas/web3-api-integrador/aportes-equipo.md`](../../entregas/web3-api-integrador/aportes-equipo.md)).

1. `git fetch && git branch -r` y revisar los pull requests abiertos. Unir con **«Create a merge commit»** los que estén bien, para que cada commit conserve a su autor.
2. **Si algún aporte no llegó** el miércoles 19/11 a la noche, seguir sin él. El Sprint 2 no depende de esos aportes. Lo único a cuidar es no editar sus archivos mientras su PR siga abierto, así no hay conflictos:

| Archivo | De quién | Qué evitar mientras su PR esté abierto |
|---|---|---|
| `Controllers/ProyectosController.cs`, `AdminUsuariosController.cs`, `AdminBitacoraController.cs` | Brandon | No tocarlos; el Sprint 2 va en controladores nuevos |
| `Datos/SemillaDatos.cs`, `appsettings.Development.json`, `apps/api/README.md` | Matías | Los proyectos publicados de la demo se crean desde la web, no desde la semilla |
| `tests/.../ProyectosCrudTests.cs` | Sergio | Las pruebas del Sprint 2 van en archivos nuevos |

## Qué falta (en este orden)

**API** (todo nuevo, sin tocar los archivos de la tabla anterior):

1. **Entidad `Inversion`** en `Dominio/Inversiones/`:
   - campos `ProyectoId`, `InversionistaId`, `MontoBs`, `Estado` (Reservada → Firmada → Pagada, o Vencida), `ReservadaHasta`, `ParticipacionPct`;
   - un `DbSet` en `AppDbContext`;
   - la migración `dotnet ef migrations add Sprint2Inversiones -o Datos/Migraciones`.
2. **RF-11 `CatalogoController`** (`api/catalogo`, roles Inversionista y Administrador):
   - `GET` devuelve los proyectos `EnFinanciamiento`, con filtros `linea`, `plazoMaximo` y `montoMinimo`;
   - cada proyecto trae lo recaudado (suma de inversiones pagadas) y el cupo libre.
3. **RF-12** `POST api/catalogo/{id}/simulacion` con `{ montoBs }` devuelve los tres escenarios, clonados con `ConfiguracionEscenario` (Prototype), y la advertencia de RNF-06. Cuando esté, RF-10 puede exigir los escenarios reales en vez de la casilla provisional.
4. **RF-13** `POST api/inversiones` con `{ proyectoId, montoBs }`:
   - 400 si el monto está bajo el mínimo;
   - 409 si supera el cupo libre (meta − pagadas − reservas vigentes);
   - si todo está bien, reserva por 15 minutos.
5. **RF-14** `POST api/inversiones/{id}/contrato`: envía un OTP con un `PropositoOtp.FirmarContrato` nuevo. Se reutiliza `OtpService`, sin cambiarlo. Después, `POST api/inversiones/{id}/contrato/confirmar` con `{ codigo }`.
6. **RF-15** `POST api/inversiones/{id}/pago` devuelve el QR simulado: el texto del cobro y su vencimiento. `POST api/inversiones/{id}/pago/confirmar` hace de aviso de la pasarela y deja la inversión en Pagada. Una reserva vencida responde 409.
7. **RF-16**: al pagar, calcular `ParticipacionPct = monto / meta`. `GET api/portafolio` devuelve las inversiones del inversionista.
8. **Pruebas** en `tests/P11.Tests/Integracion/InversionesTests.cs`: monto bajo el mínimo (400), cupo superado (409), OTP incorrecto, pago confirmado y participación, reserva vencida (con `FakeTimeProvider`, igual que en las pruebas de OTP).

**Web**: cambiar el import de `datosEjemplo.js` por llamadas a `api(...)` en `Catalogo`, `Simulador`, `Inversiones`, `Invertir`, `Contrato`, `PagoQr` y `Portafolio`. Las rutas ya existen (`casosDeUso.js`). Rondas, incidencias, expediente y liquidación siguen con datos de ejemplo, porque son del Sprint 3.

**Si no alcanza el tiempo**, el mínimo defendible es RF-11, RF-12 y RF-13 con API, y RF-14 a RF-16 con la pantalla de ejemplo. En ese caso hay que decirlo en la tabla de este README.

## Cierre del sprint

- [ ] Tabla requerimiento → endpoint → pantalla → prueba (formato del Sprint 1)
- [ ] Guion de la demo. Sugerido:
  - el analista publica «Engorde Chiquitania 2026»;
  - el inversionista lo filtra en el catálogo y simula 1.000 Bs;
  - invierte, firma con el OTP (llega a Mailpit) y paga con QR;
  - ve su participación en el portafolio;
  - el Admin repite un paso con «Ver como».
- [ ] Capturas numeradas en `capturas/` (con Brave headless y `puppeteer-core`, como en el Sprint 1)
- [ ] `dotnet test` y `npm run build` en verde
- [ ] Commit y push
