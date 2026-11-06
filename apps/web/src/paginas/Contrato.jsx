import { useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router'
import { formatoBs } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { buscarProyecto } from '../datosEjemplo'

/** RF-14: antes de pagar se aceptan el contrato y los riesgos, firmando con un código OTP (mockup 06b). */
export default function Contrato() {
  const proyecto = buscarProyecto(useParams().id)
  const monto = useLocation().state?.monto ?? 1000
  const navegar = useNavigate()
  const [acepta, setAcepta] = useState(false)
  const [codigo, setCodigo] = useState('')
  const [error, setError] = useState('')

  function continuar(e) {
    e.preventDefault()
    if (!acepta) return setError('Debes aceptar el contrato y los riesgos.')
    if (codigo.length !== 6) return setError('Ingresa el código de 6 dígitos que te enviamos por correo.')
    navegar(`/catalogo/${proyecto.id}/pago`, { state: { monto } })
  }

  return (
    <Tarjeta titulo="Contrato y riesgos" subtitulo={`${proyecto.nombre} · ${formatoBs(monto)}`}>
      <form onSubmit={continuar} className="formulario">
        <Campo etiqueta="Riesgos">
          <textarea readOnly value={proyecto.riesgos} />
        </Campo>
        <label className="casilla">
          <input type="checkbox" checked={acepta} onChange={(e) => setAcepta(e.target.checked)} />
          Leí y acepto el contrato y los riesgos
        </label>
        <Campo etiqueta="Código OTP">
          <input
            className="otp"
            inputMode="numeric"
            maxLength={6}
            placeholder="______"
            value={codigo}
            onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
          />
        </Campo>
        <button className="boton">Aceptar y continuar a pago</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
