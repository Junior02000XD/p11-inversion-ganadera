import { ADMINISTRADOR, ANALISTA, INVERSIONISTA, PRODUCTOR } from './roles'

/**
 * Pestañas del menú = casos de uso del diagrama de Proyecto de Sistemas («según el docente», D1):
 * una pestaña por CU, con su nombre exacto y en el mismo orden. Cada rol ve los CU a los que está
 * asociado; el Administrador está asociado a todos, así que ve todas las pestañas.
 * «Iniciar sesión» no es pestaña: es la pantalla pública de entrada (junto con el registro).
 *
 * `roles` = actores asociados al CU además del Administrador. `ruta` = la pestaña; las pantallas
 * internas del CU cuelgan de esa misma ruta (p. ej. /catalogo/:id), así el menú marca el CU activo.
 */
export const CASOS_DE_USO = [
  {
    id: 'identidad',
    nombre: 'Registrar cuenta y verificar identidad',
    ruta: '/identidad',
    roles: [INVERSIONISTA, PRODUCTOR, ANALISTA],
  },
  {
    id: 'usuarios',
    nombre: 'Gestionar usuarios, roles y permisos',
    ruta: '/admin/usuarios',
    roles: [],
  },
  {
    id: 'bitacora',
    nombre: 'Consultar bitácora de auditoría',
    ruta: '/admin/bitacora',
    roles: [],
  },
  {
    id: 'postular',
    nombre: 'Postular proyecto',
    ruta: '/proyectos',
    roles: [PRODUCTOR],
  },
  {
    id: 'evaluar',
    nombre: 'Evaluar y publicar proyecto',
    ruta: '/evaluacion',
    roles: [ANALISTA],
  },
  {
    id: 'catalogo',
    nombre: 'Explorar catálogo y simular escenarios',
    ruta: '/catalogo',
    roles: [INVERSIONISTA],
  },
  {
    id: 'invertir',
    nombre: 'Invertir en un proyecto',
    ruta: '/inversiones',
    roles: [INVERSIONISTA],
  },
  {
    id: 'ronda',
    nombre: 'Cerrar ronda de financiamiento',
    ruta: '/rondas',
    roles: [PRODUCTOR, INVERSIONISTA],
  },
  {
    id: 'compra',
    nombre: 'Registrar compra del ganado',
    ruta: '/compra',
    roles: [ANALISTA],
  },
  {
    id: 'traslado',
    nombre: 'Registrar traslado del ganado',
    ruta: '/traslado',
    roles: [ANALISTA],
  },
  {
    id: 'avance',
    nombre: 'Registrar avance productivo',
    ruta: '/avance',
    roles: [PRODUCTOR, ANALISTA],
  },
  {
    id: 'incidencias',
    nombre: 'Gestionar incidencias',
    ruta: '/incidencias',
    roles: [PRODUCTOR, ANALISTA],
  },
  {
    id: 'reclamos',
    nombre: 'Gestionar reclamo al seguro',
    ruta: '/reclamos',
    roles: [ANALISTA],
  },
  {
    id: 'liquidar',
    nombre: 'Liquidar proyecto',
    ruta: '/liquidacion',
    roles: [PRODUCTOR, INVERSIONISTA, ANALISTA],
  },
  {
    id: 'expediente',
    nombre: 'Consultar expediente del proyecto',
    ruta: '/expedientes',
    roles: [PRODUCTOR, INVERSIONISTA, ANALISTA],
  },
  {
    id: 'portafolio',
    nombre: 'Consultar portafolio',
    ruta: '/portafolio',
    roles: [INVERSIONISTA],
  },
]

/** Roles que pueden entrar al CU (siempre incluye al Administrador). */
export const rolesDe = (id) => [...casoDeUso(id).roles, ADMINISTRADOR]

export const casoDeUso = (id) => CASOS_DE_USO.find((c) => c.id === id)

/** Las pestañas de un rol, en el orden del diagrama. */
export const casosDe = (rol) => CASOS_DE_USO.filter((c) => rol === ADMINISTRADOR || c.roles.includes(rol))

/** Adonde se entra al iniciar sesión: la primera pestaña del rol. */
export const rutaInicial = (rol) => casosDe(rol)[0]?.ruta ?? '/ingresar'
