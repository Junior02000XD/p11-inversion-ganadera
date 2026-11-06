# Cliente web — React + Vite + JavaScript

Consume la API (`apps/api`). Requiere Node.js 20.19 o superior. En desarrollo, Vite reenvía `/api` a https://localhost:7080, así que no hace falta configurar CORS.

```bash
cd apps/web
npm install
npm run dev      # http://localhost:5173 (con la API corriendo)
npm run build    # build de producción en dist/
```

Para apuntar a otra API: `API_URL=https://otra-url npm run dev`.

## Pantallas (Sprint 1, según los mockups 01a–03b)
| Ruta | Pantalla | Rol |
|---|---|---|
| `/registro` | Crear cuenta (RF-01) | — |
| `/verificar-cuenta` | Código OTP (RNF-01) | — |
| `/ingresar` | Iniciar sesión, intentos restantes y bloqueo (RF-02) | — |
| `/inicio` | Inicio según rol (RF-06) | todos |
| `/identidad` | Verificación de identidad con consentimiento (RF-03/04, RNF-07) | Inversionista, Productor |
| `/proyectos/nuevo`, `/proyectos` | Ficha por línea en borrador y envío a evaluación (RF-07, RF-05) | Productor |
| `/documentos` | Documentos del predio (RF-05) | Productor |
| `/analista` | Revisión manual de identidad y documentos | Analista |
| `/admin/usuarios` | Usuarios y roles, desbloqueo (RF-06) | Administrador |

El token se guarda en `sessionStorage`: sobrevive a recargar la página, pero se pierde al cerrar la pestaña.

## Pantallas de los sprints 2 y 3 (mockups 04a–12c)
Ya están en la app, dentro del menú de cada rol, y se navega entre ellas como en la versión final. Todavía no tienen endpoints en la API: leen los datos de ejemplo de `src/datosEjemplo.js` y no guardan nada. Cuando exista el endpoint, cada pantalla cambia ese import por una llamada a `api(...)`.

| Ruta | Pantalla | Rol |
|---|---|---|
| `/catalogo` → `/catalogo/:id` → `…/invertir` → `…/contrato` → `…/pago` → `…/ronda` | Catálogo, simulador, monto y cupo, contrato y riesgos, pago QR, cierre de la ronda (RF-11…RF-17) | Inversionista |
| `/portafolio` → `/portafolio/:id` → `…/liquidacion` | Portafolio, expediente, conformidad y cobro (RF-22, RF-24…RF-26) | Inversionista |
| `/avance` → `/avance/pendientes` | Avance del lote y sincronización sin conexión (RF-18, RNF-04) | Productor |
| `/incidencias/nueva` → `/incidencias/:id` | Reportar incidencia y seguimiento (RF-20, RF-21) | Productor |
| `/cierre` | Venta y costos para liquidar (RF-23) | Productor |
| `/evaluacion` → `/evaluacion/:id` | Cola de evaluación, checklist y decisión (RF-08…RF-10) | Analista |
| `/operaciones/compra` → `/operaciones/traslado` | Compra del ganado y traslado con GMA (RNF-11) | Analista |
| `/liquidaciones/:id` | Liquidación calculada y aprobación (RF-23, RF-24) | Analista |
