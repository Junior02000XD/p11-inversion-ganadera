import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { api, formatoBs, formatoPct, mensajeDe, textoLinea } from '../../api'
import { Aviso, Campo, Datos, Tarjeta } from '../../componentes'

// GET  /catalogo/:id → el proyecto con sus riesgos y los supuestos del analista
// POST /catalogo/:id/simulacion { montoBs, rendimientoPct?, costosPct?, mortalidadPct?, variacionPct? }
//   → { escenarios: [{ tipo, rendimientoPct, costosPct, mortalidadPct, valorEstimadoBs, resultadoBs }], supuestosPropios, advertencia, riesgos }

// Supuestos que el inversionista puede mover. El deslizador cubre los valores habituales;
// en la casilla numérica se puede escribir cualquier valor que acepte la API.
const supuestos = [
  {
    clave: 'rendimientoPct',
    etiqueta: 'Rendimiento esperado',
    ayuda: 'Cuánto gana el proyecto sobre lo invertido en todo el plazo (venta del ganado o de la leche).',
    min: -20,
    max: 80,
    minApi: -100,
    maxApi: 300,
  },
  {
    clave: 'mortalidadPct',
    etiqueta: 'Mortalidad',
    ayuda: 'Parte del valor que se pierde por animales que mueren o se enferman.',
    min: 0,
    max: 30,
  },
  {
    clave: 'costosPct',
    etiqueta: 'Costos y comisión',
    ayuda: 'Gastos del ciclo y comisión de la plataforma, como parte de lo invertido.',
    min: 0,
    max: 40,
  },
  {
    clave: 'variacionPct',
    etiqueta: 'Variación entre escenarios',
    ayuda: 'Cuánto baja el rendimiento en el escenario pesimista y cuánto sube en el optimista.',
    min: 0,
    max: 40,
  },
]

const delAnalista = (p) => Object.fromEntries(supuestos.map((s) => [s.clave, String(p[s.clave] ?? 0)]))

// La API devuelve los errores por campo en PascalCase («RendimientoPct»).
const pascal = (clave) => clave.charAt(0).toUpperCase() + clave.slice(1)

/** RF-12: tres escenarios (pesimista, esperado, optimista) que el inversionista ajusta moviendo los supuestos (mockup 05b). */
export default function Simulador() {
  const { id } = useParams()
  const [proyecto, setProyecto] = useState(null)
  const [monto, setMonto] = useState('1000')
  const [valores, setValores] = useState(null)
  const [simulacion, setSimulacion] = useState(null)
  const [error, setError] = useState('')
  const [campos, setCampos] = useState({})

  useEffect(() => {
    api(`/catalogo/${id}`)
      .then((p) => {
        setProyecto(p)
        setMonto(String(Math.max(1000, p.minimoBs)))
        setValores(delAnalista(p))
      })
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  // Se recalcula solo, un momento después de que el usuario deja de mover los valores.
  useEffect(() => {
    if (!valores) return
    if (!(Number(monto) > 0)) {
      setSimulacion(null)
      setCampos({ MontoBs: ['Indica un monto mayor a 0.'] })
      return
    }
    let vigente = true
    const espera = setTimeout(async () => {
      const cuerpo = { montoBs: Number(monto) }
      for (const s of supuestos) if (valores[s.clave] !== '') cuerpo[s.clave] = Number(valores[s.clave])
      try {
        const r = await api(`/catalogo/${id}/simulacion`, { cuerpo })
        if (!vigente) return
        setSimulacion(r)
        setCampos({})
        setError('')
      } catch (err) {
        if (!vigente) return
        // Sin resultado viejo en pantalla: con un dato inválido no hay escenarios que mostrar.
        setSimulacion(null)
        setCampos(err.campos ?? {})
        setError(Object.keys(err.campos ?? {}).length ? '' : mensajeDe(err))
      }
    }, 300)
    return () => {
      vigente = false
      clearTimeout(espera)
    }
  }, [id, monto, valores])

  if (!proyecto || !valores)
    return <Tarjeta titulo="Simulador">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  const cambiar = (clave, valor) => setValores({ ...valores, [clave]: valor })
  const cambiados = supuestos.some((s) => valores[s.clave] !== '' && Number(valores[s.clave]) !== Number(proyecto[s.clave] ?? 0))
  const mayorResultado = Math.max(1, ...(simulacion?.escenarios.map((e) => Math.abs(e.resultadoBs)) ?? []))

  return (
    <Tarjeta titulo="Simulador" subtitulo={`${proyecto.nombre} · ${textoLinea(proyecto.linea)} · ${proyecto.ubicacion}`} ancha>
      <Datos
        filas={[
          ['Meta de financiamiento', formatoBs(proyecto.metaBs)],
          ['Cupo libre (lo que falta reunir)', formatoBs(proyecto.cupoLibreBs)],
          ['Inversión mínima', formatoBs(proyecto.minimoBs)],
          ['Plazo', `${proyecto.plazoMeses} meses`],
          ['Riesgo', proyecto.nivelRiesgo],
        ]}
      />
      <Aviso tipo="info">Riesgos: {proyecto.riesgos}</Aviso>

      <div className="formulario">
        <Campo etiqueta="Monto a invertir (Bs)" ayuda={`Desde ${formatoBs(proyecto.minimoBs)} hasta el cupo libre.`} error={campos.MontoBs}>
          <input type="number" min="1" value={monto} onChange={(e) => setMonto(e.target.value)} />
        </Campo>

        <fieldset className="grupo">
          <legend>Supuestos del escenario esperado (%)</legend>
          <p className="nota">
            Empiezan con los valores del analista que revisó el proyecto. Muévelos para ver qué pasa si el negocio sale mejor o peor.
          </p>
          <div className="formulario">
            {supuestos.map((s) => (
              <Campo key={s.clave} etiqueta={s.etiqueta} ayuda={s.ayuda} error={campos[pascal(s.clave)]}>
                <div className="control-supuesto">
                  <input
                    type="range"
                    min={s.min}
                    max={s.max}
                    step="0.5"
                    aria-label={`${s.etiqueta} (deslizador)`}
                    value={Math.min(s.max, Math.max(s.min, Number(valores[s.clave] === '' ? (proyecto[s.clave] ?? 0) : valores[s.clave])))}
                    onChange={(e) => cambiar(s.clave, e.target.value)}
                  />
                  <input
                    type="number"
                    min={s.minApi ?? 0}
                    max={s.maxApi ?? 100}
                    step="0.5"
                    aria-label={`${s.etiqueta} (%)`}
                    value={valores[s.clave]}
                    onChange={(e) => cambiar(s.clave, e.target.value)}
                  />
                </div>
              </Campo>
            ))}
          </div>
          {cambiados && (
            <div className="acciones-supuestos">
              <span className="chip">Supuestos cambiados por ti</span>
              <button type="button" className="boton-texto" onClick={() => setValores(delAnalista(proyecto))}>
                Volver a los del analista
              </button>
            </div>
          )}
        </fieldset>
      </div>
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
                  <th>Ganancia o pérdida</th>
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
                    <td>
                      {(e.resultadoBs >= 0 ? '+' : '−') + formatoBs(Math.abs(e.resultadoBs))}
                      <span className="tenue"> ({(e.resultadoBs >= 0 ? '+' : '−') + formatoPct(Math.abs((e.resultadoBs / simulacion.montoBs) * 100))})</span>
                      <div className={`progreso${e.resultadoBs < 0 ? ' progreso-negativo' : ''}`} aria-hidden>
                        <div style={{ width: `${(Math.abs(e.resultadoBs) / mayorResultado) * 100}%` }} />
                      </div>
                    </td>
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
