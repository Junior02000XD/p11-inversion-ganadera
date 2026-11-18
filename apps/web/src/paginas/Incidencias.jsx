import { Link } from 'react-router'
import { Tarjeta } from '../componentes'
import { buscarProyecto, incidencias } from '../datosEjemplo'
import { PRODUCTOR } from '../roles'
import { useSesion } from '../sesion'

/** CU «Gestionar incidencias» (RF-20): el productor reporta y el analista hace el seguimiento. */
export default function Incidencias() {
  const { usuario } = useSesion()
  return (
    <Tarjeta titulo="Incidencias" subtitulo="Mortalidad, enfermedad, robo o clima, con su evidencia." ancha>
      {usuario?.rol === PRODUCTOR && (
        <Link className="boton" to="/incidencias/nueva">
          Reportar incidencia
        </Link>
      )}
      <ul className="lista">
        {incidencias.map((i) => (
          <li key={i.id} className="item">
            <div className="item-cabecera">
              <strong>
                #{i.id} · {i.tipo}
              </strong>
              <span className={`chip ${i.estado === 'Cerrada' ? 'chip-Aprobado' : ''}`}>{i.estado}</span>
            </div>
            <div className="tenue">
              {buscarProyecto(i.proyectoId).nombre} · {i.detalle} · {i.fecha}
            </div>
            <Link to={`/incidencias/${i.id}`}>Ver seguimiento</Link>
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
