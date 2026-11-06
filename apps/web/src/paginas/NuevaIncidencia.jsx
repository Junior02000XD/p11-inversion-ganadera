import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Archivo, Aviso, Campo, Tarjeta } from '../componentes'

/** RF-20: el productor reporta mortalidad, enfermedad, robo o clima, con evidencia (mockup 10a). */
export default function NuevaIncidencia() {
  const navegar = useNavigate()
  const [foto, setFoto] = useState(null)
  const [error, setError] = useState('')

  function enviar(e) {
    e.preventDefault()
    if (!foto) return setError('Adjunta al menos una evidencia.')
    navegar('/incidencias/12')
  }

  return (
    <Tarjeta titulo="Nueva incidencia" subtitulo="Engorde Chiquitania 2026">
      <form onSubmit={enviar} className="formulario">
        <Campo etiqueta="Tipo">
          <select>
            <option>Mortalidad</option>
            <option>Enfermedad</option>
            <option>Robo</option>
            <option>Clima</option>
          </select>
        </Campo>
        <Campo etiqueta="Cabezas afectadas">
          <input type="number" min="0" placeholder="2" />
        </Campo>
        <Campo etiqueta="Descripción">
          <textarea placeholder="Dos animales con fiebre" />
        </Campo>
        <Campo etiqueta="Foto de evidencia">
          <Archivo texto="Tomar o elegir foto" accept="image/jpeg,image/png" alElegir={setFoto} />
        </Campo>
        <button className="boton">Enviar reporte</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
