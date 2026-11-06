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
            <Link className="boton boton-secundario" to="/avance">
              Registrar avance del lote
            </Link>
            <Link className="boton boton-secundario" to="/incidencias/nueva">
              Reportar incidencia
            </Link>
          </>
        )}
        {usuario.rol === 'Inversionista' && (
          <>
            <Link className="boton" to="/catalogo">
              Explorar proyectos
            </Link>
            <Link className="boton" to="/portafolio">
              Mi portafolio
            </Link>
          </>
        )}
        {usuario.rol === 'Analista' && (
          <>
            <Link className="boton" to="/analista">
              Identidad y documentos pendientes
            </Link>
            <Link className="boton" to="/evaluacion">
              Proyectos por evaluar
            </Link>
            <Link className="boton boton-secundario" to="/liquidaciones/chiquitania">
              Liquidaciones
            </Link>
          </>
        )}
        {usuario.rol === 'Administrador' && (
          <Link className="boton" to="/admin/usuarios">
            Usuarios y roles
          </Link>
        )}
      </nav>
    </Tarjeta>
  )
}
