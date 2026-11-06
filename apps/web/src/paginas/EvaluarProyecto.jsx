import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { porEvaluar } from '../datosEjemplo'

const puntos = [
  'Documentos del predio verificados',
  'Registro RUNSA vigente',
  'Plan productivo coherente',
  'Riesgos identificados',
  'Tres escenarios completos',
]

/** RF-08, RF-09 y RF-10: checklist técnico y decisión; sin riesgos ni escenarios no se publica (mockup 04b). */
export default function EvaluarProyecto() {
  const { id } = useParams()
  const proyecto = porEvaluar.find((p) => p.id === id) ?? porEvaluar[0]
  const navegar = useNavigate()
  const [marcados, setMarcados] = useState(puntos.slice(0, 2))
  const [observaciones, setObservaciones] = useState('')
  const [error, setError] = useState('')

  const marcar = (punto) =>
    setMarcados(marcados.includes(punto) ? marcados.filter((m) => m !== punto) : [...marcados, punto])

  function decidir(decision) {
    setError('')
    if (decision === 'publicado' && !(marcados.includes('Riesgos identificados') && marcados.includes('Tres escenarios completos')))
      return setError('Faltan riesgos o escenarios: no es publicable.')
    if (decision !== 'publicado' && !observaciones.trim()) return setError('Escribe las observaciones para el productor.')
    navegar('/evaluacion', { state: { evaluado: proyecto.id, decision: `«${proyecto.nombre}» quedó ${decision}.` } })
  }

  return (
    <Tarjeta titulo={`Evaluación técnica: ${proyecto.nombre}`} subtitulo={`Productor: ${proyecto.productor}`}>
      <div className="formulario">
        {puntos.map((punto) => (
          <label key={punto} className="casilla">
            <input type="checkbox" checked={marcados.includes(punto)} onChange={() => marcar(punto)} />
            {punto}
          </label>
        ))}
        <Campo etiqueta="Observaciones">
          <textarea placeholder="Falta detallar la alimentación" value={observaciones} onChange={(e) => setObservaciones(e.target.value)} />
        </Campo>
        <button className="boton" onClick={() => decidir('publicado')}>
          Aprobar y publicar
        </button>
        <button className="boton boton-secundario" onClick={() => decidir('observado')}>
          Observar
        </button>
        <button className="boton boton-peligro" onClick={() => decidir('rechazado')}>
          Rechazar
        </button>
        <Aviso>{error}</Aviso>
      </div>
    </Tarjeta>
  )
}
