import { Link, useLocation, useParams } from 'react-router'
import { formatoBs } from '../../api'
import { Aviso, Progreso, Tarjeta } from '../../componentes'
import { buscarProyecto } from '../../datosEjemplo'

/** RF-16 y RF-17: cómo cerró la ronda: financiada (mockup 07a) o sin llegar al mínimo (mockup 07b). */
export default function Ronda() {
  const proyecto = buscarProyecto(useParams().id)
  const monto = useLocation().state?.monto ?? 1000

  if (!proyecto.financiado) {
    return (
      <Tarjeta titulo="Ronda cerrada sin fondos" subtitulo={proyecto.nombre}>
        <div className="formulario">
          <span>No alcanzó el mínimo</span>
          <Progreso porcentaje={(proyecto.recaudadoBs / proyecto.metaBs) * 100} />
          <Aviso tipo="info">Se devolverán tus aportes: {formatoBs(monto)}.</Aviso>
          <Link className="boton" to="/catalogo">
            Volver al catálogo
          </Link>
        </div>
      </Tarjeta>
    )
  }

  const participacion = ((monto / proyecto.metaBs) * 100).toLocaleString('es-BO', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
  return (
    <Tarjeta titulo="Ronda cerrada" subtitulo={proyecto.nombre}>
      <div className="formulario">
        <span>Financiamiento alcanzado</span>
        <Progreso porcentaje={100} />
        <Aviso tipo="ok">
          Tu participación: {formatoBs(monto)} ({participacion} %)
        </Aviso>
        <span className="tenue">El productor inicia la compra del ganado.</span>
        <Link className="boton" to={`/expedientes/${proyecto.id}`}>
          Ver expediente
        </Link>
      </div>
    </Tarjeta>
  )
}
