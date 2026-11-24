import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, formatoBs, formatoPct, mensajeDe } from '../../api'
import { Aviso, Tarjeta } from '../../componentes'

// GET /portafolio → { totalInvertidoBs, participaciones: [{ id, proyecto, montoBs, participacionPct, participacionKg, codigoConstancia }] }

/** RF-16 (y la base de RF-26): participaciones pagadas del inversionista, con su constancia (mockup 11a). */
export default function Portafolio() {
  const [portafolio, setPortafolio] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api('/portafolio')
      .then(setPortafolio)
      .catch((err) => setError(mensajeDe(err)))
  }, [])

  if (!portafolio) return <Tarjeta titulo="Mi portafolio">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  return (
    <Tarjeta titulo="Mi portafolio" ancha>
      {portafolio.participaciones.length === 0 ? (
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
                <th>Participación</th>
                <th>Constancia</th>
              </tr>
            </thead>
            <tbody>
              {portafolio.participaciones.map((p) => (
                <tr key={p.id}>
                  <td>{p.proyecto}</td>
                  <td>{formatoBs(p.montoBs)}</td>
                  <td>
                    {formatoPct(p.participacionPct)}
                    {p.participacionKg && <div className="tenue">≈ {p.participacionKg.toLocaleString('es-BO')} kg</div>}
                  </td>
                  <td>
                    <Link to={`/portafolio/${p.id}`}>{p.codigoConstancia}</Link>
                  </td>
                </tr>
              ))}
              <tr>
                <td>
                  <strong>Total invertido</strong>
                </td>
                <td colSpan={3}>
                  <strong>{formatoBs(portafolio.totalInvertidoBs)}</strong>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </Tarjeta>
  )
}
