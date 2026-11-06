// Cliente de la API: agrega el token y convierte las respuestas de error en mensajes legibles.

export type Rol = 'Inversionista' | 'Productor' | 'Analista' | 'Administrador'
export type EstadoVerificacion = 'Pendiente' | 'EnRevision' | 'Verificado' | 'Rechazado'

export interface UsuarioActual {
  id: string
  nombre: string
  correo: string
  rol: Rol
  estadoVerificacion: EstadoVerificacion
  consintio: boolean
}

export interface Respuesta {
  mensaje: string
}

export class ErrorApi extends Error {
  constructor(
    public status: number,
    mensaje: string,
    /** Cuerpo de la respuesta, para leer campos extra como `intentosRestantes`. */
    public datos: Record<string, unknown> = {},
    /** Errores de validación por campo (`errors` de ASP.NET Core). */
    public campos: Record<string, string[]> = {},
  ) {
    super(mensaje)
  }
}

// El token vive en sessionStorage: sobrevive a recargar la página, no a cerrar la pestaña.
// Si el navegador bloquea el almacenamiento, la sesión queda solo en memoria.
const CLAVE_TOKEN = 'p11.token'
let token: string | null = leerToken()

function leerToken(): string | null {
  try {
    return sessionStorage.getItem(CLAVE_TOKEN)
  } catch {
    return null
  }
}

export function guardarToken(nuevo: string | null) {
  token = nuevo
  try {
    if (nuevo) sessionStorage.setItem(CLAVE_TOKEN, nuevo)
    else sessionStorage.removeItem(CLAVE_TOKEN)
  } catch {
    // almacenamiento bloqueado: seguimos solo en memoria
  }
}

export const hayToken = () => token !== null

async function pedir(ruta: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  try {
    return await fetch(`/api${ruta}`, { ...init, headers })
  } catch {
    throw new ErrorApi(0, 'No se pudo conectar con el servidor.')
  }
}

async function error(r: Response, ruta: string): Promise<ErrorApi> {
  // Un 401 fuera del login significa que el token venció o es inválido.
  if (r.status === 401 && !ruta.startsWith('/auth/')) guardarToken(null)
  const datos = await r.json().catch(() => ({}))
  const campos: Record<string, string[]> = datos.errors ?? {}
  const mensaje =
    datos.mensaje ??
    (Object.keys(campos).length > 0 ? 'Revisa los campos marcados.' : null) ??
    (r.status === 403 ? 'Tu rol no tiene permiso para esta acción.' : `Error ${r.status}`)
  return new ErrorApi(r.status, mensaje, datos, campos)
}

/** Llama a la API. Con `cuerpo` envía JSON; con `form`, multipart (archivos). */
export async function api<T>(ruta: string, opciones: { metodo?: string; cuerpo?: unknown; form?: FormData } = {}): Promise<T> {
  const init: RequestInit = { method: opciones.metodo ?? 'GET' }
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
  return (r.status === 204 ? {} : await r.json()) as T
}

/** Descarga un archivo protegido y lo abre en una pestaña nueva. */
export async function abrirArchivo(ruta: string) {
  const r = await pedir(ruta)
  if (!r.ok) throw await error(r, ruta)
  const url = URL.createObjectURL(await r.blob())
  window.open(url, '_blank', 'noopener')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const mensajeDe = (err: unknown) => (err instanceof ErrorApi ? err.message : 'Ocurrió un error inesperado.')

export const textoEstado: Record<EstadoVerificacion, string> = {
  Pendiente: 'Pendiente',
  EnRevision: 'En revisión',
  Verificado: 'Verificado',
  Rechazado: 'Rechazado',
}

export const formatoBs = (n: number) => `Bs ${n.toLocaleString('es-BO', { maximumFractionDigits: 2 })}`
export const formatoFecha = (iso: string) => new Date(iso).toLocaleString('es-BO', { dateStyle: 'short', timeStyle: 'short' })
export const formatoHora = (iso: string) => new Date(iso).toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false })
