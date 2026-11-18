import { useState } from 'react'
import { ESTILOS, aplicarEstilo, estiloGuardado } from './estilo'
import { Link, Navigate, NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import { casosDe, rutaInicial } from './casosDeUso'
import { ADMINISTRADOR } from './roles'
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

/**
 * Protege rutas: sin sesión va al login; con un rol que no está asociado al caso de uso, a su primera
 * pestaña (RF-06). El Administrador está asociado a todos los casos de uso, así que siempre pasa.
 */
export function RequiereSesion({ roles }) {
  const { usuario, cargando } = useSesion()
  if (cargando) return <p className="cargando">Cargando…</p>
  if (!usuario) return <Navigate to="/ingresar" replace />
  if (roles && usuario.rol !== ADMINISTRADOR && !roles.includes(usuario.rol)) return <Navigate to={rutaInicial(usuario.rol)} replace />
  return <Outlet />
}

/** Lleva a la primera pestaña del rol (después de iniciar sesión, o ante una ruta desconocida). */
export function IrAlInicio() {
  const { usuario, cargando } = useSesion()
  if (cargando) return <p className="cargando">Cargando…</p>
  return <Navigate to={usuario ? rutaInicial(usuario.rol) : '/ingresar'} replace />
}

/**
 * Un caso de uso con varios actores muestra a cada uno su parte (p. ej. en «Liquidar proyecto» el
 * productor carga la venta, el analista aprueba y el inversionista da conformidad).
 * `vistas` = [{ roles, etiqueta, elemento, admin? }]. El Administrador ve un selector «Ver como» con
 * todas las vistas que puede operar (`admin: false` = la vista es de la propia cuenta, no aplica).
 */
export function VistaPorRol({ vistas }) {
  const { usuario } = useSesion()
  const deAdmin = vistas.filter((v) => v.admin !== false)
  const [elegida, setElegida] = useState(0)
  if (!usuario) return null

  if (usuario.rol !== ADMINISTRADOR) {
    const vista = vistas.find((v) => v.roles.includes(usuario.rol))
    return vista ? vista.elemento : null
  }
  if (deAdmin.length === 1) return deAdmin[0].elemento
  const actual = deAdmin[Math.min(elegida, deAdmin.length - 1)]
  return (
    <div className="vista-por-rol">
      <div className="ver-como" role="tablist" aria-label="Ver como">
        <span>Ver como</span>
        {deAdmin.map((v, i) => (
          <button key={v.etiqueta} type="button" role="tab" aria-selected={actual === v} className={actual === v ? 'activo' : ''} onClick={() => setElegida(i)}>
            {v.etiqueta}
          </button>
        ))}
      </div>
      {actual.elemento}
    </div>
  )
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
  const { pathname } = useLocation()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const [rutaVista, setRutaVista] = useState(pathname)
  // En celular el menú se pliega solo al cambiar de pantalla.
  if (rutaVista !== pathname) {
    setRutaVista(pathname)
    setMenuAbierto(false)
  }
  const casos = usuario ? casosDe(usuario.rol) : []

  return (
    <div className={`marco${usuario ? ' marco-con-menu' : ''}`}>
      <header className="barra">
        <div className="barra-fila">
          {usuario && (
            <button type="button" className="boton-menu" aria-expanded={menuAbierto} aria-controls="menu-casos" onClick={() => setMenuAbierto(!menuAbierto)}>
              <span aria-hidden>☰</span> Menú
            </button>
          )}
          <Link to={usuario ? rutaInicial(usuario.rol) : '/ingresar'} className="marca">
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
      </header>
      <div className="cuerpo">
        {usuario && (
          <nav id="menu-casos" className={`menu-lateral${menuAbierto ? ' abierto' : ''}`} aria-label="Casos de uso">
            <p className="menu-titulo">Casos de uso</p>
            {casos.map((c) => (
              <NavLink key={c.id} to={c.ruta}>
                {c.nombre}
              </NavLink>
            ))}
          </nav>
        )}
        <main className="contenido">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
