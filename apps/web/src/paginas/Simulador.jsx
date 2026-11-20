import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { api, formatoBs, formatoPct, mensajeDe, textoLinea } from '../api'
import { Aviso, Campo, Datos, Tarjeta } from '../componentes'

// GET  /catalogo/:id → el proyecto con sus riesgos y supuestos
// POST /catalogo/:id/simulacion { montoBs } → { escenarios: [{ tipo, rendimientoPct, costosPct, mortalidadPct, valorEstimadoBs, resultadoBs }], advertencia, riesgos }

/** RF-12: tres escenarios (pesimista, esperado, optimista) para el monto que se quiere invertir (mockup 05b). */
export default function Simulador() {
  const { id } = useParams()
  const [proyecto, setProyecto] = useState(null)
  const [monto, setMonto] = useState('1000')
  const [simulacion, setSimulacion] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api(`/catalogo/${id}`)
      .then(setProyecto)
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  async function calcular(e) {
    e.preventDefault()
    setError('')
    try {
      setSimulacion(await api(`/catalogo/${id}/simulacion`, { cuerpo: { montoBs: Number(monto) } }))
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  if (!proyecto) return <Tarjeta titulo="Simulador">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  return (
    <Tarjeta titulo="Simulador" subtitulo={`${proyecto.nombre} · ${textoLinea(proyecto.linea)} · ${proyecto.ubicacion}`} ancha>
      <Datos
        filas={[
          ['Meta', formatoBs(proyecto.metaBs)],
          ['Cupo libre', formatoBs(proyecto.cupoLibreBs)],
          ['Inversión mínima', formatoBs(proyecto.minimoBs)],
          ['Plazo', `${proyecto.plazoMeses} meses`],
          ['Riesgo', proyecto.nivelRiesgo],
        ]}
      />
      <Aviso tipo="info">Riesgos: {proyecto.riesgos}</Aviso>

      <form onSubmit={calcular} className="formulario">
        <Campo etiqueta="Monto a simular (Bs)">
          <input type="number" min="1" value={monto} onChange={(e) => setMonto(e.target.value)} />
        </Campo>
        <button className="boton">Calcular escenarios</button>
      </form>
      <Aviso>{error}</Aviso>

      {simulacion && (
        <>
          <div className="tabla-scroll">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Escenario</th>
                  <th>Supuestos</th>
                  <th>Valor al final</th>
                  <th>Resultado</th>
                </tr>
              </thead>
              <tbody>
                {simulacion.escenarios.map((e) => (
                  <tr key={e.tipo}>
                    <td>
                      <strong>{e.tipo}</strong>
                    </td>
                    <td className="tenue">
                      rendimiento {formatoPct(e.rendimientoPct)} · costos {formatoPct(e.costosPct)} · mortalidad {formatoPct(e.mortalidadPct)}
                    </td>
                    <td>{formatoBs(e.valorEstimadoBs)}</td>
                    <td>{(e.resultadoBs >= 0 ? '+' : '−') + formatoBs(Math.abs(e.resultadoBs))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* RNF-06: toda rentabilidad se muestra como estimación, nunca como garantizada. */}
          <Aviso tipo="info">{simulacion.advertencia}</Aviso>
        </>
      )}
      <div className="menu">
        <Link className="boton" to={`/inversiones/nueva/${proyecto.id}`}>
          Invertir en este proyecto
        </Link>
      </div>
    </Tarjeta>
  )
}
