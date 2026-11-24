# Cliente web — React + Vite + JavaScript

Consume la API (`apps/api`). Requiere Node.js 20.19 o superior. En desarrollo, Vite reenvía `/api` a https://localhost:7080, así que no hace falta configurar CORS.

```bash
cd apps/web
npm install
npm run dev      # http://localhost:5173 (con la API corriendo)
npm run build    # build de producción en dist/
```

Para apuntar a otra API: `API_URL=https://otra-url npm run dev`.

## Pantallas

El menú lateral tiene una pestaña por caso de uso del diagrama D1, con su nombre y en el mismo orden. La lista sale de [`src/casosDeUso.js`](src/casosDeUso.js), que también decide qué roles entran a cada una (el Administrador entra a todas). Las rutas están en [`src/main.jsx`](src/main.jsx) y las pantallas en `src/pages/`, una carpeta por módulo.

Públicas: `/ingresar` (RF-02), `/registro` (RF-01) y `/verificar-cuenta` (OTP, RNF-01).

| Caso de uso | Rutas | Roles (además del Administrador) | Datos |
|---|---|---|---|
| Registrar cuenta y verificar identidad | `/identidad` | Inversionista, Productor, Analista | API |
| Gestionar usuarios, roles y permisos | `/admin/usuarios` | — | API |
| Consultar bitácora de auditoría | `/admin/bitacora` | — | API |
| Postular proyecto | `/proyectos`, `/proyectos/nuevo`, `/proyectos/:id/editar`, `/proyectos/documentos` | Productor | API |
| Evaluar y publicar proyecto | `/evaluacion`, `/evaluacion/:id` | Analista | API |
| Explorar catálogo y simular escenarios | `/catalogo`, `/catalogo/:id` | Inversionista | API |
| Invertir en un proyecto | `/inversiones`, `/inversiones/nueva/:proyectoId`, `/inversiones/:id/contrato`, `/inversiones/:id/pago` | Inversionista | API |
| Cerrar ronda de financiamiento | `/rondas`, `/rondas/:id` | Productor, Inversionista | Ejemplo |
| Registrar compra del ganado | `/compra` | Analista | Ejemplo |
| Registrar traslado del ganado | `/traslado` | Analista | Ejemplo |
| Registrar avance productivo | `/avance`, `/avance/pendientes` | Productor, Analista | Ejemplo |
| Gestionar incidencias | `/incidencias`, `/incidencias/nueva`, `/incidencias/:id` | Productor, Analista | Ejemplo |
| Gestionar reclamo al seguro | `/reclamos` | Analista | Ejemplo |
| Liquidar proyecto | `/liquidacion` | Productor, Inversionista, Analista | Ejemplo |
| Consultar expediente del proyecto | `/expedientes`, `/expedientes/:id` | Productor, Inversionista, Analista | Ejemplo |
| Consultar portafolio | `/portafolio`, `/portafolio/:id` | Inversionista | API |

**Datos**: «API» = la pantalla lee y guarda con la API. «Ejemplo» = la pantalla lee `src/datosEjemplo.js` y no guarda nada (son las de `pages/operations/` y `pages/closing/`); cuando exista su endpoint, cambia ese import por una llamada a `api(...)`.

En los casos de uso con varios actores, `VistaPorRol` muestra la vista de cada rol, y el Administrador elige cuál con «Ver como».

El token se guarda en `sessionStorage`: sobrevive a recargar la página, pero se pierde al cerrar la pestaña.
