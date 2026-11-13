import { useState } from 'react'
import { ESTILOS, aplicarEstilo, estiloGuardado } from './estilo'
import { Link, Navigate, NavLink, Outlet, useNavigate } from 'react-router'
import { useSesion } from './sesion'

/** Mensaje de error, de éxito o informativo. Si no hay texto, no muestra nada. */
export function Aviso({ tipo = 'error', children }) {
  if (!children) return null
  return (
    <div className={`aviso aviso-${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      <span className="aviso-icono" aria-hidden>
        {tipo === 'ok' ? '✓' : '!'}
      </span>
      <span>{children}</span>
    </div>
  )
}

/** Etiqueta + control de formulario, con texto de ayuda y errores de validación opcionales. */
export function Campo({ etiqueta, error, children, ayuda }) {
  return (
    <label className={`campo${error?.length ? ' campo-error' : ''}`}>
      <span className="campo-etiqueta">{etiqueta}</span>
      {children}
      {ayuda && <span className="campo-ayuda">{ayuda}</span>}
      {error?.map((e) => (
        <span key={e} className="campo-msj">
          {e}
        </span>
      ))}
    </label>
  )
}

export function Tarjeta({ titulo, subtitulo, children, ancha }) {
  return (
    <section className={`tarjeta${ancha ? ' tarjeta-ancha' : ''}`}>
      <h1>{titulo}</h1>
      {subtitulo && <p className="subtitulo">{subtitulo}</p>}
      {children}
    </section>
  )
}

/** Protege rutas: sin sesión va al login; con rol equivocado, al inicio (RF-06). */
export function RequiereSesion({ roles }) {
  const { usuario, cargando } = useSesion()
  if (cargando) return <p className="cargando">Cargando…</p>
  if (!usuario) return <Navigate to="/ingresar" replace />
  if (roles && !roles.includes(usuario.rol)) return <Navigate to="/inicio" replace />
  return <Outlet />
}

/** Lista de dos columnas: etiqueta a la izquierda, valor a la derecha. `filas` es [[etiqueta, valor], ...]. */
export function Datos({ filas, total }) {
  return (
    <dl className="datos">
      {filas.map(([etiqueta, valor]) => (
        <div key={etiqueta}>
          <dt>{etiqueta}</dt>
          <dd>{valor}</dd>
        </div>
      ))}
      {total && (
        <div className="datos-total">
          <dt>{total[0]}</dt>
          <dd>{total[1]}</dd>
        </div>
      )}
    </dl>
  )
}

/** Barra de avance, de 0 a 100. */
export function Progreso({ porcentaje }) {
  return (
    <div className="progreso" role="progressbar" aria-valuenow={Math.round(porcentaje)} aria-valuemin={0} aria-valuemax={100}>
      <div style={{ width: `${Math.min(porcentaje, 100)}%` }} />
    </div>
  )
}

/** Selector de archivo con el nombre del elegido. Avisa al padre con `alElegir(archivo)`. */
export function Archivo({ texto, accept = 'image/jpeg,image/png,application/pdf', alElegir }) {
  const [nombre, setNombre] = useState('')
  return (
    <div className={`archivo${nombre ? ' archivo-cargado' : ''}`}>
      <input
        type="file"
        accept={accept}
        onChange={(e) => {
          const archivo = e.target.files?.[0]
          setNombre(archivo?.name ?? '')
          alElegir?.(archivo)
        }}
      />
      <span>{nombre || texto}</span>
    </div>
  )
}

// Secciones del menú de cada rol (RF-06).
const menus = {
  Inversionista: [
    ['/inicio', 'Inicio'],
    ['/catalogo', 'Catálogo'],
    ['/portafolio', 'Portafolio'],
    ['/identidad', 'Identidad'],
  ],
  Productor: [
    ['/inicio', 'Inicio'],
    ['/proyectos', 'Mis proyectos'],
    ['/documentos', 'Documentos'],
    ['/avance', 'Avance'],
    ['/incidencias/nueva', 'Incidencias'],
    ['/cierre', 'Cierre'],
  ],
  Analista: [
    ['/inicio', 'Inicio'],
    ['/analista', 'Identidad y documentos'],
    ['/evaluacion', 'Evaluación'],
    ['/operaciones/compra', 'Compra y traslado'],
    ['/liquidaciones/chiquitania', 'Liquidaciones'],
  ],
  Administrador: [
    ['/inicio', 'Inicio'],
    ['/admin/usuarios', 'Usuarios y roles'],
  ],
}

/** Cambia entre los dos estilos rústicos (Pampa verde y Cuero y madera). */
function SelectorEstilo() {
  const [estilo, setEstilo] = useState(estiloGuardado)
  const elegir = (id) => {
    aplicarEstilo(id)
    setEstilo(id)
  }
  return (
    <div className="selector-estilo" role="group" aria-label="Estilo de la página">
      {ESTILOS.map((e) => (
        <button key={e.id} type="button" aria-pressed={estilo === e.id} onClick={() => elegir(e.id)}>
          {e.nombre}
        </button>
      ))}
    </div>
  )
}

export function Marco() {
  const { usuario, salir } = useSesion()
  const navegar = useNavigate()
  return (
    <div className="marco">
      <header className="barra">
        <div className="barra-fila">
          <Link to={usuario ? '/inicio' : '/ingresar'} className="marca">
            <span className="marca-logo" aria-hidden>
              P11
            </span>
            Inversión Ganadera
          </Link>
          <div className="barra-usuario">
            <SelectorEstilo />
            {usuario && <span className="barra-rol">{usuario.rol}</span>}
            {usuario && (
              <button
                className="boton-texto"
                onClick={() => {
                  salir()
                  navegar('/ingresar')
                }}
              >
                Salir
              </button>
            )}
          </div>
        </div>
        {usuario && (
          <nav className="navegacion">
            {menus[usuario.rol].map(([ruta, texto]) => (
              <NavLink key={ruta} to={ruta}>
                {texto}
              </NavLink>
            ))}
          </nav>
        )}
      </header>
      <main className="contenido">
        <Outlet />
      </main>
    </div>
  )
}
