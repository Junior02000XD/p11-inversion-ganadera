import { Link } from 'react-router'
import { formatoBs } from '../api'
import { Tarjeta } from '../componentes'
import { buscarProyecto, portafolio } from '../datosEjemplo'

/** RF-26: participaciones del inversionista (mockup 11a). */
export default function Portafolio() {
  const total = portafolio.reduce((suma, p) => suma + p.montoBs, 0)

  return (
    <Tarjeta titulo="Mi portafolio" ancha>
      {portafolio.length === 0 ? (
        <p className="tenue">
          Sin proyectos aún: explora el <Link to="/catalogo">catálogo</Link>.
        </p>
      ) : (
        <div className="tabla-scroll">
          <table className="tabla">
            <thead>
              <tr>
                <th>Proyecto</th>
                <th>Aporte</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {portafolio.map((p) => (
                <tr key={p.proyectoId}>
                  <td>
                    <Link to={`/portafolio/${p.proyectoId}`}>{buscarProyecto(p.proyectoId).nombre}</Link>
                  </td>
                  <td>{formatoBs(p.montoBs)}</td>
                  <td>
                    <span className="chip chip-Aprobado">{p.estado}</span>
                  </td>
                </tr>
              ))}
              <tr>
                <td>
                  <strong>Total invertido</strong>
                </td>
                <td colSpan={2}>
                  <strong>{formatoBs(total)}</strong>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </Tarjeta>
  )
}
