# Corrección: pestañas = casos de uso (18/11/2026)

El ingeniero de Proyecto de Sistemas pidió dos cambios:

- **a)** el Administrador accede a todos los módulos y puede operar en todos;
- **b)** las pestañas que ve cada usuario al entrar son una réplica 1:1 de sus casos de uso.

Se tomó como fuente el diagrama **D1 «según el docente»** ([`../../diagramas/uml/01-casos-de-uso-sistema-v1-docente.mdj`](../../diagramas/uml/01-casos-de-uso-sistema-v1-docente.mdj)). Tiene 17 CU y el Administrador está conectado a todos.

## Cómo quedó

- **Zona pública:** «Iniciar sesión» y «Registrarse» son la entrada, no pestañas.
- **Menú lateral:** al entrar, cada rol ve solo las pestañas de sus CU, con el nombre exacto del CU y en el orden del diagrama. Cae directo en la primera; no hay página de Inicio.
- **Administrador:** ve las 16 pestañas.
  - En los CU con varios actores tiene un selector **«Ver como»** (Productor · Analista · Inversionista), así opera cualquiera de las partes.
  - Puede postular un proyecto en nombre de un productor.
  - Puede eliminar cuentas: si la cuenta tiene proyectos, la API no la borra y sugiere bloquearla.
- **Fuente única:** el catálogo de CU está en [`apps/web/src/casosDeUso.js`](../../../apps/web/src/casosDeUso.js). De ahí salen el menú, los permisos de cada ruta y la pantalla de entrada. En la API, los controladores de analista y productor también aceptan al Administrador.

| Caso de uso (D1) | Pestaña | Inversionista | Productor | Analista | Admin |
|---|---|:-:|:-:|:-:|:-:|
| Registrar cuenta y verificar identidad | `/identidad` | ✓ | ✓ | ✓ (revisión manual) | ✓ |
| Gestionar usuarios, roles y permisos | `/admin/usuarios` | | | | ✓ |
| Consultar bitácora de auditoría | `/admin/bitacora` | | | | ✓ |
| Postular proyecto | `/proyectos` | | ✓ | | ✓ |
| Evaluar y publicar proyecto | `/evaluacion` | | | ✓ | ✓ |
| Explorar catálogo y simular escenarios | `/catalogo` | ✓ | | | ✓ |
| Invertir en un proyecto | `/inversiones` | ✓ | | | ✓ |
| Cerrar ronda de financiamiento | `/rondas` | ✓ | ✓ | | ✓ |
| Registrar compra del ganado | `/compra` | | | ✓ | ✓ |
| Registrar traslado del ganado | `/traslado` | | | ✓ | ✓ |
| Registrar avance productivo | `/avance` | | ✓ | ✓ | ✓ |
| Gestionar incidencias | `/incidencias` | | ✓ | ✓ | ✓ |
| Gestionar reclamo al seguro | `/reclamos` | | | ✓ | ✓ |
| Liquidar proyecto | `/liquidacion` | ✓ | ✓ | ✓ | ✓ |
| Consultar expediente del proyecto | `/expedientes` | ✓ | ✓ | ✓ | ✓ |
| Consultar portafolio | `/portafolio` | ✓ | | | ✓ |

Las pantallas internas de cada CU cuelgan de su pestaña (por ejemplo `/catalogo/:id`, `/inversiones/:id/pago` o `/proyectos/:id/editar`), así el menú marca el CU en el que estás.

## Capturas

| Archivo | Qué muestra |
|---|---|
| `capturas/correccion-01-admin-menu.png` | Administrador con sus 16 pestañas |
| `capturas/correccion-02-admin-ver-como.png` | «Liquidar proyecto» con el selector «Ver como» |
| `capturas/correccion-03-analista-menu.png` | Analista: 9 CU |
| `capturas/correccion-04-productor-menu.png` | Productor: 7 CU |
| `capturas/correccion-05-inversionista-menu.png` | Inversionista: 7 CU |
| `capturas/correccion-06-admin-celular.png` | Menú desplegado en celular |
