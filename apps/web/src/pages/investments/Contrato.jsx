import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, formatoBs, mensajeDe } from '../../api'
import { Aviso, Campo, CuentaRegresiva, Tarjeta } from '../../componentes'

// GET  /inversiones/:id/contrato           → { version, texto, riesgos, montoBs, reservadaHasta }
// POST /inversiones/:id/contrato/codigo    → envía el OTP al correo
// POST /inversiones/:id/contrato/firmar    { aceptaRiesgos, codigo } → la inversión queda «Firmada»

/** RF-14: antes de pagar se aceptan el contrato y los riesgos, firmando con un código OTP (mockup 06b). */
export default function Contrato() {
  const { id } = useParams()
  const navegar = useNavigate()
  const [contrato, setContrato] = useState(null)
  const [acepta, setAcepta] = useState(false)
  const [codigo, setCodigo] = useState('')
  const [codigoEnviado, setCodigoEnviado] = useState('')
  const [vencida, setVencida] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    api(`/inversiones/${id}/contrato`)
      .then(setContrato)
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  async function pedirCodigo() {
    setError('')
    try {
      setCodigoEnviado((await api(`/inversiones/${id}/contrato/codigo`, { metodo: 'POST' })).mensaje)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  async function firmar(e) {
    e.preventDefault()
    setError('')
    if (!acepta) return setError('Debes aceptar el contrato y los riesgos.')
    try {
      await api(`/inversiones/${id}/contrato/firmar`, { cuerpo: { aceptaRiesgos: acepta, codigo } })
      navegar(`/inversiones/${id}/pago`)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  if (!contrato) return <Tarjeta titulo="Contrato y riesgos">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  return (
    <Tarjeta titulo="Contrato y riesgos" subtitulo={`${formatoBs(contrato.montoBs)} · versión ${contrato.version}`}>
      <form onSubmit={firmar} className="formulario">
        <CuentaRegresiva hasta={contrato.reservadaHasta} alVencer={() => setVencida(true)} />
        {vencida && <Aviso>La reserva venció y el cupo se liberó. Vuelve al catálogo para reservar de nuevo.</Aviso>}
        <div className="contrato">{contrato.texto}</div>
        <label className="casilla">
          <input type="checkbox" checked={acepta} onChange={(e) => setAcepta(e.target.checked)} />
          Leí y acepto el contrato y los riesgos
        </label>
        <button type="button" className="boton boton-secundario" onClick={pedirCodigo} disabled={vencida}>
          {codigoEnviado ? 'Enviarme otro código' : 'Enviarme el código por correo'}
        </button>
        <Aviso tipo="ok">{codigoEnviado}</Aviso>
        <Campo etiqueta="Código OTP" ayuda="6 dígitos. Vence a los 10 minutos y tienes 3 intentos.">
          <input
            className="otp"
            inputMode="numeric"
            maxLength={6}
            placeholder="______"
            value={codigo}
            onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
          />
        </Campo>
        <button className="boton" disabled={vencida || codigo.length !== 6}>
          Firmar y continuar al pago
        </button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
