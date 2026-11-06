import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { formatoBs } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { buscarProyecto } from '../datosEjemplo'

/** RF-13: el monto debe superar el mínimo y entrar en el cupo; si cumple, se reserva por 30 minutos (mockup 06a). */
export default function Invertir() {
  const proyecto = buscarProyecto(useParams().id)
  const navegar = useNavigate()
  const [monto, setMonto] = useState('')
  const [error, setError] = useState('')
  const cupo = proyecto.metaBs - proyecto.recaudadoBs

  function reservar(e) {
    e.preventDefault()
    const valor = Number(monto)
    if (valor < proyecto.minimoBs) return setError(`El mínimo es ${formatoBs(proyecto.minimoBs)}.`)
    if (valor > cupo) return setError('El monto excede el cupo disponible.')
    navegar(`/catalogo/${proyecto.id}/contrato`, { state: { monto: valor } })
  }

  return (
    <Tarjeta titulo="Invertir" subtitulo={proyecto.nombre}>
      <form onSubmit={reservar} className="formulario">
        <Campo etiqueta="Monto (Bs)" ayuda={`Mínimo ${formatoBs(proyecto.minimoBs)} · Cupo disponible ${formatoBs(cupo)}`}>
          <input type="number" placeholder="1.000" value={monto} onChange={(e) => setMonto(e.target.value)} />
        </Campo>
        <Aviso>{error}</Aviso>
        <button className="boton">Reservar cupo por 30 min</button>
      </form>
    </Tarjeta>
  )
}
