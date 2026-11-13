import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoProyecto } from '../api'
import { Aviso, Tarjeta } from '../componentes'

// GET /analista/proyectos devuelve los proyectos en revisión técnica:
// [{ proyecto: { id, nombre, linea, montoRequeridoBs, estado, creadoEn, ... }, productor, correoProductor }]

/** RF-09: proyectos enviados a evaluación técnica (mockup 04a). */
export default function Evaluacion() {
  const { decision } = useLocation().state ?? {}
  const [pendientes, setPendientes] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api('/analista/proyectos')
      .then(setPendientes)
      .catch((err) => setError(mensajeDe(err)))
  }, [])

  return (
    <Tarjeta titulo="Proyectos por evaluar" ancha>
      {decision && <Aviso tipo="ok">{decision}</Aviso>}
      <Aviso>{error}</Aviso>
      {pendientes?.length === 0 && <p className="tenue">No hay proyectos esperando evaluación.</p>}
      {pendientes?.length > 0 && (
        <div className="tabla-scroll">
          <table className="tabla">
            <thead>
              <tr>
                <th>Proyecto</th>
                <th>Productor</th>
                <th>Monto</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pendientes.map(({ proyecto: p, productor }) => (
                <tr key={p.id}>
                  <td>
                    <strong>{p.nombre}</strong>
                    <div className="tenue">
                      {p.linea === 'Lecheria' ? 'Lechería' : 'Engorde'} · creado {formatoFecha(p.creadoEn)}
                    </div>
                  </td>
                  <td>{productor}</td>
                  <td>{formatoBs(p.montoRequeridoBs)}</td>
                  <td>
                    <span className={`chip chip-${p.estado}`}>{textoEstadoProyecto[p.estado]}</span>
                  </td>
                  <td>
                    <Link to={`/evaluacion/${p.id}`}>Evaluar</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Tarjeta>
  )
}
