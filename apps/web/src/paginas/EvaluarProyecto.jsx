import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoProyecto } from '../api'
import { Aviso, Campo, Datos, Tarjeta } from '../componentes'

// GET /analista/proyectos/:id → { proyecto, productor, correoProductor, historial: [{ estadoAnterior, estadoNuevo, accion, responsable, motivo, fecha }] }
// POST /analista/proyectos/:id/decision { decision: 'Aprobar' | 'Observar' | 'Rechazar', observaciones, riesgosCargados, tresEscenarios }
// Si el estado del proyecto no admite la decisión, la API responde 409 con el motivo.

const RIESGOS = 'Riesgos identificados'
const ESCENARIOS = 'Tres escenarios completos'
// Los tres primeros puntos son la guía del analista; los dos últimos los exige la API para publicar (RF-10).
const puntos = ['Documentos del predio verificados', 'Registro RUNSA vigente', 'Plan productivo coherente', RIESGOS, ESCENARIOS]

const textoDecision = { Aprobar: 'quedó publicado', Observar: 'volvió al productor con observaciones', Rechazar: 'quedó rechazado' }

/** RF-08, RF-09 y RF-10: checklist técnico y decisión; sin riesgos ni escenarios no se publica (mockup 04b). */
export default function EvaluarProyecto() {
  const { id } = useParams()
  const navegar = useNavigate()
  const [datos, setDatos] = useState(null)
  const [marcados, setMarcados] = useState([])
  const [observaciones, setObservaciones] = useState('')
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
          riesgosCargados: marcados.includes(RIESGOS),
          tresEscenarios: marcados.includes(ESCENARIOS),
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
    ['Línea', p.linea === 'Lecheria' ? 'Lechería' : 'Engorde'],
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
          ['Costos estimados', formatoBs(p.costosEstimadosBs)],
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
