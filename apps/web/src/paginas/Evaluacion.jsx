import { Link, useLocation } from 'react-router'
import { Aviso, Tarjeta } from '../componentes'
import { porEvaluar } from '../datosEjemplo'

/** RF-09: proyectos enviados a evaluación técnica (mockup 04a). */
export default function Evaluacion() {
  // Al volver de una evaluación, ese proyecto ya no está pendiente.
  const { evaluado, decision } = useLocation().state ?? {}
  const pendientes = porEvaluar.filter((p) => p.id !== evaluado)

  return (
    <Tarjeta titulo="Proyectos por evaluar" ancha>
      {decision && <Aviso tipo="ok">{decision}</Aviso>}
      <div className="tabla-scroll">
        <table className="tabla">
          <thead>
            <tr>
              <th>Proyecto</th>
              <th>Productor</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {pendientes.map((p) => (
              <tr key={p.id}>
                <td>
                  <strong>{p.nombre}</strong>
                </td>
                <td>{p.productor}</td>
                <td>
                  <span className={p.estado === 'Observado' ? 'chip chip-Pendiente' : 'chip chip-EnRevision'}>{p.estado}</span>
                </td>
                <td>
                  <Link to={`/evaluacion/${p.id}`}>Abrir expediente</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Tarjeta>
  )
}
