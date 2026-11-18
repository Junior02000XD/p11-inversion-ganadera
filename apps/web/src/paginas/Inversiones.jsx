import { Link } from 'react-router'
import { formatoBs } from '../api'
import { Progreso, Tarjeta } from '../componentes'
import { portafolio, proyectos } from '../datosEjemplo'

/** CU «Invertir en un proyecto» (RF-13 a RF-16): proyectos con la ronda abierta y lo ya invertido. */
export default function Inversiones() {
  const invertido = (id) => portafolio.find((p) => p.proyectoId === id)?.montoBs
  return (
    <Tarjeta titulo="Invertir en un proyecto" subtitulo="Elige el monto, firma el contrato con un código y paga con QR." ancha>
      <ul className="lista">
        {proyectos.map((p) => (
          <li key={p.id} className="item">
            <div className="item-cabecera">
              <strong>{p.nombre}</strong>
              <span className="tenue">cierra {p.cierre}</span>
            </div>
            <Progreso porcentaje={(p.recaudadoBs / p.metaBs) * 100} />
            <div className="tenue">
              {formatoBs(p.recaudadoBs)} de {formatoBs(p.metaBs)} · mínimo {formatoBs(p.minimoBs)}
              {invertido(p.id) && ` · ya invertiste ${formatoBs(invertido(p.id))}`}
            </div>
            <Link className="boton" to={`/inversiones/${p.id}`}>
              Invertir
            </Link>
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
