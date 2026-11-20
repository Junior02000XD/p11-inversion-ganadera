// Cliente de la API: agrega el token y convierte las respuestas de error en mensajes legibles.
//
// Roles: 'Inversionista' | 'Productor' | 'Analista' | 'Administrador'
// Estados de verificación: 'Pendiente' | 'EnRevision' | 'Verificado' | 'Rechazado'
// El usuario actual (GET /auth/yo) llega como { id, nombre, correo, rol, estadoVerificacion, consintio }.

export class ErrorApi extends Error {
  /**
   * @param status código HTTP (0 si no hubo conexión)
   * @param mensaje texto para mostrar al usuario
   * @param datos cuerpo de la respuesta, para leer campos extra como `intentosRestantes`
   * @param campos errores de validación por campo (`errors` de ASP.NET Core)
   */
  constructor(status, mensaje, datos = {}, campos = {}) {
    super(mensaje)
    this.status = status
    this.datos = datos
    this.campos = campos
  }
}

// El token vive en sessionStorage: sobrevive a recargar la página, no a cerrar la pestaña.
// Si el navegador bloquea el almacenamiento, la sesión queda solo en memoria.
const CLAVE_TOKEN = 'p11.token'
let token = leerToken()

function leerToken() {
  try {
    return sessionStorage.getItem(CLAVE_TOKEN)
  } catch {
    return null
  }
}

export function guardarToken(nuevo) {
  token = nuevo
  try {
    if (nuevo) sessionStorage.setItem(CLAVE_TOKEN, nuevo)
    else sessionStorage.removeItem(CLAVE_TOKEN)
  } catch {
    // almacenamiento bloqueado: seguimos solo en memoria
  }
}

export const hayToken = () => token !== null

async function pedir(ruta, init = {}) {
  const headers = new Headers(init.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  try {
    return await fetch(`/api${ruta}`, { ...init, headers })
  } catch {
    throw new ErrorApi(0, 'No se pudo conectar con el servidor.')
  }
}

async function error(r, ruta) {
  // Un 401 fuera del login significa que el token venció o es inválido.
  if (r.status === 401 && !ruta.startsWith('/auth/')) guardarToken(null)
  const datos = await r.json().catch(() => ({}))
  const campos = datos.errors ?? {}
  const mensaje =
    datos.mensaje ??
    (Object.keys(campos).length > 0 ? 'Revisa los campos marcados.' : null) ??
    (r.status === 403 ? 'Tu rol no tiene permiso para esta acción.' : `Error ${r.status}`)
  return new ErrorApi(r.status, mensaje, datos, campos)
}

/** Llama a la API. Con `cuerpo` envía JSON; con `form`, multipart (archivos). */
export async function api(ruta, opciones = {}) {
  const init = { method: opciones.metodo ?? 'GET' }
  if (opciones.form) {
    init.method = opciones.metodo ?? 'POST'
    init.body = opciones.form
  } else if (opciones.cuerpo !== undefined) {
    init.method = opciones.metodo ?? 'POST'
    init.headers = { 'Content-Type': 'application/json' }
    init.body = JSON.stringify(opciones.cuerpo)
  }

  const r = await pedir(ruta, init)
  if (!r.ok) throw await error(r, ruta)
  return r.status === 204 ? {} : await r.json()
}

/** Descarga un archivo protegido y lo abre en una pestaña nueva. */
export async function abrirArchivo(ruta) {
  const r = await pedir(ruta)
  if (!r.ok) throw await error(r, ruta)
  const url = URL.createObjectURL(await r.blob())
  window.open(url, '_blank', 'noopener')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const mensajeDe = (err) => (err instanceof ErrorApi ? err.message : 'Ocurrió un error inesperado.')

export const textoEstado = {
  Pendiente: 'Pendiente',
  EnRevision: 'En revisión',
  Verificado: 'Verificado',
  Rechazado: 'Rechazado',
}

// Estados del proyecto (EST-3): 'Borrador' | 'EnRevision' | 'Rechazado' | 'EnFinanciamiento'
export const textoEstadoProyecto = {
  Borrador: 'Borrador',
  EnRevision: 'En revisión técnica',
  Rechazado: 'Rechazado',
  EnFinanciamiento: 'En financiamiento',
}

export const formatoBs = (n) => `Bs ${n.toLocaleString('es-BO', { maximumFractionDigits: 2 })}`
export const formatoFecha = (iso) => new Date(iso).toLocaleString('es-BO', { dateStyle: 'short', timeStyle: 'short' })
export const formatoHora = (iso) => new Date(iso).toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false })

export const textoLinea = (linea) => (linea === 'Lecheria' ? 'Lechería' : 'Engorde')

// Estados de una inversión (RF-13 a RF-16): 'Reservada' | 'Firmada' | 'Pagada' | 'Vencida' | 'Rechazada'
export const textoEstadoInversion = {
  Reservada: 'Cupo reservado',
  Firmada: 'Contrato firmado',
  Pagada: 'Pagada',
  Vencida: 'Reserva vencida',
  Rechazada: 'Pago rechazado',
}

export const formatoPct = (n) => `${Number(n).toLocaleString('es-BO', { maximumFractionDigits: 2 })} %`
