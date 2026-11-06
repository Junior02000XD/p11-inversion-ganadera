import { Link } from 'react-router'
import { textoEstado } from '../api'
import { Aviso, Tarjeta } from '../componentes'
import { useSesion } from '../sesion'

/** Inicio según rol (mockup 02b): las opciones cambian con el rol (RF-06). */
export default function Inicio() {
  const { usuario } = useSesion()
  if (!usuario) return null
  const nombre = usuario.nombre.split(' ')[0] || usuario.correo
  const operador = usuario.rol === 'Inversionista' || usuario.rol === 'Productor'
  const verificado = usuario.estadoVerificacion === 'Verificado'

  return (
    <Tarjeta titulo={`Hola, ${nombre}`} subtitulo={`Rol: ${usuario.rol}`}>
      {operador && (
        <div className={`estado estado-${usuario.estadoVerificacion}`}>
          <span>Identidad</span>
          <strong>{textoEstado[usuario.estadoVerificacion]}</strong>
        </div>
      )}
      {operador && !verificado && (
        <Aviso tipo="info">
          {usuario.estadoVerificacion === 'EnRevision'
            ? 'Un analista está revisando tu identidad. Te avisamos por correo.'
            : 'Hasta verificar tu identidad no puedes invertir ni publicar proyectos.'}
        </Aviso>
      )}
      <nav className="menu">
        {operador && (
          <Link className="boton" to="/identidad">
            {verificado ? 'Mi verificación' : 'Verificar identidad'}
          </Link>
        )}
        {usuario.rol === 'Productor' && (
          <>
            <Link className="boton" to="/proyectos/nuevo">
              Nuevo proyecto
            </Link>
            <Link className="boton" to="/proyectos">
              Mis proyectos
            </Link>
            <Link className="boton" to="/documentos">
              Documentos del predio
            </Link>
          </>
        )}
        {usuario.rol === 'Inversionista' && (
          <>
            <button className="boton" disabled title="Sprint 2">
              Explorar proyectos
            </button>
            <button className="boton" disabled title="Sprint 3">
              Mi portafolio
            </button>
          </>
        )}
        {usuario.rol === 'Analista' && (
          <Link className="boton" to="/analista">
            Revisiones pendientes
          </Link>
        )}
        {usuario.rol === 'Administrador' && (
          <Link className="boton" to="/admin/usuarios">
            Usuarios y roles
          </Link>
        )}
      </nav>
      {usuario.rol === 'Inversionista' && <p className="nota">El catálogo y el portafolio llegan en los próximos sprints.</p>}
    </Tarjeta>
  )
}
