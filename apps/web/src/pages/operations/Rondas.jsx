import { Link } from 'react-router'
import { formatoBs } from '../../api'
import { Progreso, Tarjeta } from '../../componentes'
import { proyectos } from '../../datosEjemplo'

/**
 * CU «Cerrar ronda de financiamiento» (RF-17): al llegar la fecha de cierre, la ronda se cierra sola;
 * financiada si alcanzó la meta, sin fondos (con devolución) si no. Productor e inversionista ven el resultado.
 */
export default function Rondas() {
  return (
    <Tarjeta titulo="Rondas de financiamiento" subtitulo="La ronda se cierra en su fecha; si no llega al mínimo, se devuelven los aportes." ancha>
      <ul className="lista">
        {proyectos.map((p) => (
          <li key={p.id} className="item">
            <div className="item-cabecera">
              <strong>{p.nombre}</strong>
              <span className={`chip ${p.financiado ? 'chip-Aprobado' : 'chip-Rechazado'}`}>{p.financiado ? 'Financiada' : 'Sin fondos'}</span>
            </div>
            <Progreso porcentaje={(p.recaudadoBs / p.metaBs) * 100} />
            <div className="tenue">
              {formatoBs(p.recaudadoBs)} de {formatoBs(p.metaBs)} · cierre {p.cierre}
            </div>
            <Link className="boton boton-secundario" to={`/rondas/${p.id}`}>
              Ver cierre
            </Link>
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
