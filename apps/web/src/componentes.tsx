import type { ReactNode } from 'react'
import { Link, Navigate, Outlet, useNavigate } from 'react-router'
import type { Rol } from './api'
import { useSesion } from './sesion'

export function Aviso({ tipo = 'error', children }: { tipo?: 'error' | 'ok' | 'info'; children: ReactNode }) {
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

export function Campo({ etiqueta, error, children, ayuda }: { etiqueta: string; error?: string[]; children: ReactNode; ayuda?: string }) {
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

export function Tarjeta({ titulo, subtitulo, children, ancha }: { titulo: string; subtitulo?: ReactNode; children: ReactNode; ancha?: boolean }) {
  return (
    <section className={`tarjeta${ancha ? ' tarjeta-ancha' : ''}`}>
      <h1>{titulo}</h1>
      {subtitulo && <p className="subtitulo">{subtitulo}</p>}
      {children}
    </section>
  )
}

/** Protege rutas: sin sesión va al login; con rol equivocado, al inicio (RF-06). */
export function RequiereSesion({ roles }: { roles?: Rol[] }) {
  const { usuario, cargando } = useSesion()
  if (cargando) return <p className="cargando">Cargando…</p>
  if (!usuario) return <Navigate to="/ingresar" replace />
  if (roles && !roles.includes(usuario.rol)) return <Navigate to="/inicio" replace />
  return <Outlet />
}

export function Marco() {
  const { usuario, salir } = useSesion()
  const navegar = useNavigate()
  return (
    <div className="marco">
      <header className="barra">
        <Link to={usuario ? '/inicio' : '/ingresar'} className="marca">
          <span className="marca-logo" aria-hidden>
            P11
          </span>
          Inversión Ganadera
        </Link>
        {usuario && (
          <div className="barra-usuario">
            <span className="barra-rol">{usuario.rol}</span>
            <button
              className="boton-texto"
              onClick={() => {
                salir()
                navegar('/ingresar')
              }}
            >
              Salir
            </button>
          </div>
        )}
      </header>
      <main className="contenido">
        <Outlet />
      </main>
    </div>
  )
}
