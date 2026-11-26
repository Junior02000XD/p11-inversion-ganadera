import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoProyecto, textoLinea } from '../../api'
import { Aviso, Campo, Datos, Tarjeta } from '../../componentes'

// GET /analista/proyectos/:id → { proyecto, productor, correoProductor, historial: [{ estadoAnterior, estadoNuevo, accion, responsable, motivo, fecha }] }
// POST /analista/proyectos/:id/decision { decision: 'Aprobar' | 'Observar' | 'Rechazar', observaciones,
//   y para aprobar: nivelRiesgo, riesgos, rendimientoPct, costosPct, mortalidadPct, variacionPct }
// Si el estado del proyecto no admite la decisión, o falta algún dato para publicar, la API responde 409.

// Guía del analista: no se envía a la API, solo ordena la revisión.
const puntos = ['Documentos del predio verificados', 'Registro RUNSA vigente', 'Plan productivo coherente']

// RF-10: supuestos del escenario esperado. El simulador (RF-12) arma con ellos los tres escenarios.
const supuestos = [
  { clave: 'rendimientoPct', etiqueta: 'Rendimiento esperado (%)', ayuda: 'Ganancia bruta sobre lo invertido, en todo el plazo.', placeholder: '22' },
  {
    clave: 'costosPct',
    etiqueta: 'Costos y comisión (%)',
    ayuda: 'Gastos del ciclo y comisión de la plataforma, como parte de lo invertido. Sirven de guía los costos estimados de la ficha.',
    placeholder: '8',
  },
  {
    clave: 'mortalidadPct',
    etiqueta: 'Pérdida por mortalidad (%)',
    ayuda: 'Parte del valor que se pierde por animales que mueren o se enferman.',
    placeholder: '2',
  },
  { clave: 'variacionPct', etiqueta: 'Variación entre escenarios (%)', ayuda: 'Cuánto baja (pesimista) o sube (optimista) el rendimiento.', placeholder: '8' },
]

const textoDecision = { Aprobar: 'quedó publicado', Observar: 'volvió al productor con observaciones', Rechazar: 'quedó rechazado' }

const numero = (texto) => (texto === '' ? null : Number(texto))

/** RF-08, RF-09 y RF-10: checklist técnico y decisión; sin riesgos ni supuestos de los escenarios no se publica (mockup 04b). */
export default function EvaluarProyecto() {
  const { id } = useParams()
  const navegar = useNavigate()
  const [datos, setDatos] = useState(null)
  const [marcados, setMarcados] = useState([])
  const [observaciones, setObservaciones] = useState('')
  const [nivelRiesgo, setNivelRiesgo] = useState('')
  const [riesgos, setRiesgos] = useState('')
  const [valores, setValores] = useState({ rendimientoPct: '', costosPct: '', mortalidadPct: '', variacionPct: '' })
  const [error, setError] = useState('')
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    api(`/analista/proyectos/${id}`)
      .then(setDatos)
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  const marcar = (punto) =>
    setMarcados(marcados.includes(punto) ? marcados.filter((m) => m !== punto) : [...marcados, punto])

  async function decidir(decision) {
    setError('')
    setEnviando(true)
    try {
      await api(`/analista/proyectos/${id}/decision`, {
        cuerpo: {
          decision,
          observaciones,
          nivelRiesgo: nivelRiesgo || null,
          riesgos,
          rendimientoPct: numero(valores.rendimientoPct),
          costosPct: numero(valores.costosPct),
          mortalidadPct: numero(valores.mortalidadPct),
          variacionPct: numero(valores.variacionPct),
        },
      })
      navegar('/evaluacion', { state: { decision: `«${datos.proyecto.nombre}» ${textoDecision[decision]}.` } })
    } catch (err) {
      setError(mensajeDe(err))
    } finally {
      setEnviando(false)
    }
  }

  if (!datos) return <Tarjeta titulo="Evaluación técnica">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  const p = datos.proyecto
  const filas = [
    ['Estado', textoEstadoProyecto[p.estado]],
    ['Línea', textoLinea(p.linea)],
    ['Ubicación', p.ubicacion],
    ['Monto requerido', formatoBs(p.montoRequeridoBs)],
    ['Inversión mínima', formatoBs(p.montoMinimoBs)],
    ['Plazo', `${p.plazoMeses} meses`],
    ...(p.linea === 'Lecheria'
      ? [
          ['Vacas en ordeño', p.vacasEnOrdeno],
          ['Litros por día', p.litrosPorDia],
          ['Precio del litro', formatoBs(p.precioLitroBs)],
        ]
      : [
          ['Cabezas', p.cantidadAnimales],
          ['Peso inicial → meta', `${p.pesoInicialKg} → ${p.pesoMetaKg} kg`],
          ['Costos estimados del ciclo (los declara el productor)', formatoBs(p.costosEstimadosBs)],
        ]),
  ]

  return (
    <Tarjeta titulo={`Evaluación técnica: ${p.nombre}`} subtitulo={`Productor: ${datos.productor} (${datos.correoProductor})`}>
      <Datos filas={filas} />
      <div className="formulario">
        {puntos.map((punto) => (
          <label key={punto} className="casilla">
            <input type="checkbox" checked={marcados.includes(punto)} onChange={() => marcar(punto)} />
            {punto}
          </label>
        ))}
        <h2>Para publicar: riesgos y escenarios</h2>
        <Campo etiqueta="Nivel de riesgo" ayuda="Lo ve el inversionista en el catálogo. Bajo: ganado sano, predio en regla y supuestos conservadores. Medio: algún factor que vigilar (clima, precio). Alto: varios factores o poca experiencia del productor.">
          <select value={nivelRiesgo} onChange={(e) => setNivelRiesgo(e.target.value)}>
            <option value="">Elegir…</option>
            <option value="Bajo">Bajo</option>
            <option value="Medio">Medio</option>
            <option value="Alto">Alto</option>
          </select>
        </Campo>
        <Campo etiqueta="Riesgos del proyecto" ayuda="Los lee el inversionista antes de invertir y van en el contrato.">
          <textarea placeholder="Mortalidad, enfermedades o caída del precio de venta." value={riesgos} onChange={(e) => setRiesgos(e.target.value)} />
        </Campo>
        <div className="filtros">
          {supuestos.map((s) => (
            <Campo key={s.clave} etiqueta={s.etiqueta} ayuda={s.ayuda}>
              <input
                type="number"
                step="0.1"
                placeholder={s.placeholder}
                value={valores[s.clave]}
                onChange={(e) => setValores({ ...valores, [s.clave]: e.target.value })}
              />
            </Campo>
          ))}
        </div>
        <Campo etiqueta="Observaciones" ayuda="Obligatorias para observar o rechazar; le llegan al productor por correo.">
          <textarea placeholder="Falta detallar la alimentación" value={observaciones} onChange={(e) => setObservaciones(e.target.value)} />
        </Campo>
        <button className="boton" disabled={enviando} onClick={() => decidir('Aprobar')}>
          Aprobar y publicar
        </button>
        <button className="boton boton-secundario" disabled={enviando} onClick={() => decidir('Observar')}>
          Observar
        </button>
        <button className="boton boton-peligro" disabled={enviando} onClick={() => decidir('Rechazar')}>
          Rechazar
        </button>
        <Aviso>{error}</Aviso>
      </div>

      {datos.historial.length > 0 && (
        <>
          <h2>Historial</h2>
          <ol className="historial">
            {datos.historial.map((h, i) => (
              <li key={i}>
                <div>
                  <strong>{h.accion}</strong> → {textoEstadoProyecto[h.estadoNuevo]} <span className="tenue">· {formatoFecha(h.fecha)}</span>
                </div>
                <div className="tenue">
                  {h.motivo} — {h.responsable}
                </div>
              </li>
            ))}
          </ol>
        </>
      )}
    </Tarjeta>
  )
}
