import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Archivo, Aviso, Campo, Tarjeta } from '../componentes'

/** RF-18: el productor registra el avance del lote; sin señal se guarda en el teléfono (RNF-04, mockup 08a). */
export default function Avance() {
  const navegar = useNavigate()
  const [sinConexion, setSinConexion] = useState(true)
  const [cabezas, setCabezas] = useState('')
  const [peso, setPeso] = useState('')
  const [error, setError] = useState('')

  function guardar(e) {
    e.preventDefault()
    if (!cabezas) return setError('Campo obligatorio: cabezas vivas.')
    if (!peso) return setError('Campo obligatorio: peso promedio.')
    navegar('/avance/pendientes', { state: { nuevo: sinConexion } })
  }

  return (
    <Tarjeta titulo="Avance del lote" subtitulo="Engorde Chiquitania 2026">
      <form onSubmit={guardar} className="formulario">
        <label className="casilla">
          <input type="checkbox" checked={sinConexion} onChange={(e) => setSinConexion(e.target.checked)} />
          Sin conexión (se guarda en el teléfono)
        </label>
        <Campo etiqueta="Cabezas vivas">
          <input type="number" min="0" placeholder="118" value={cabezas} onChange={(e) => setCabezas(e.target.value)} />
        </Campo>
        <Campo etiqueta="Peso promedio (kg)">
          <input type="number" min="0" placeholder="340" value={peso} onChange={(e) => setPeso(e.target.value)} />
        </Campo>
        <Campo etiqueta="Observaciones">
          <textarea placeholder="Buen apetito" />
        </Campo>
        <Campo etiqueta="Foto de evidencia">
          <Archivo texto="Tomar o elegir foto" accept="image/jpeg,image/png" />
        </Campo>
        <button className="boton">Guardar avance</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
