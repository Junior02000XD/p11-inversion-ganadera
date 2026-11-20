import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoInversion } from '../api'
import { Aviso, Progreso, Tarjeta } from '../componentes'

// GET /catalogo     → proyectos con la ronda abierta
// GET /inversiones  → mis inversiones: [{ id, proyecto, montoBs, estado, reservadaHasta, codigoConstancia, ... }]

/** Adónde sigue una inversión según su estado: firmar, pagar o ver la constancia. */
const siguientePaso = (i) =>
  ({
    Reservada: { texto: 'Firmar contrato', ruta: `/inversiones/${i.id}/contrato` },
    Firmada: { texto: 'Pagar con QR', ruta: `/inversiones/${i.id}/pago` },
    Pagada: { texto: 'Ver constancia', ruta: `/portafolio/${i.id}` },
  })[i.estado]

/** CU «Invertir en un proyecto» (RF-13 a RF-16): proyectos con la ronda abierta y mis inversiones. */
export default function Inversiones() {
  const [proyectos, setProyectos] = useState(null)
  const [mias, setMias] = useState([])
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([api('/catalogo'), api('/inversiones')])
      .then(([catalogo, inversiones]) => {
        setProyectos(catalogo)
        setMias(inversiones)
      })
      .catch((err) => setError(mensajeDe(err)))
  }, [])

  return (
    <div className="pila">
      <Tarjeta titulo="Invertir en un proyecto" subtitulo="Elige el monto, firma el contrato con un código y paga con QR." ancha>
        <Aviso>{error}</Aviso>
        {proyectos?.length === 0 && <p className="tenue">No hay proyectos con la ronda abierta.</p>}
        <ul className="lista">
          {proyectos?.map((p) => (
            <li key={p.id} className="item">
              <div className="item-cabecera">
                <strong>{p.nombre}</strong>
                <span className="tenue">cierra {formatoFecha(p.cierreRondaEn)}</span>
              </div>
              <Progreso porcentaje={p.porcentajeFinanciado} />
              <div className="tenue">
                {formatoBs(p.recaudadoBs)} de {formatoBs(p.metaBs)} · mínimo {formatoBs(p.minimoBs)} · cupo libre {formatoBs(p.cupoLibreBs)}
              </div>
              <Link className="boton" to={`/inversiones/nueva/${p.id}`}>
                Invertir
              </Link>
            </li>
          ))}
        </ul>
      </Tarjeta>

      {mias.length > 0 && (
        <Tarjeta titulo="Mis inversiones" ancha>
          <div className="tabla-scroll">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Proyecto</th>
                  <th>Monto</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {mias.map((i) => (
                  <tr key={i.id}>
                    <td>
                      {i.proyecto}
                      <div className="tenue">{formatoFecha(i.creadaEn)}</div>
                    </td>
                    <td>{formatoBs(i.montoBs)}</td>
                    <td>
                      <span className={`chip chip-${i.estado}`}>{textoEstadoInversion[i.estado]}</span>
                    </td>
                    <td>{siguientePaso(i) && <Link to={siguientePaso(i).ruta}>{siguientePaso(i).texto}</Link>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Tarjeta>
      )}
    </div>
  )
}
