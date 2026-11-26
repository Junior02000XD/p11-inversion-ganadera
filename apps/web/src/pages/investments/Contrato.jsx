import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, formatoBs, mensajeDe } from '../../api'
import { Aviso, Campo, CuentaRegresiva, Tarjeta } from '../../componentes'

// GET  /inversiones/:id/contrato           → { version, texto, riesgos, montoBs, reservadaHasta }
// POST /inversiones/:id/contrato/codigo    { aceptaRiesgos } → envía el OTP al correo (400 si no aceptó)
// POST /inversiones/:id/contrato/firmar    { aceptaRiesgos, codigo } → la inversión queda «Firmada»

/**
 * RF-14: antes de pagar se aceptan el contrato y los riesgos, firmando con un código OTP (mockup 06b).
 * Va por pasos: ① leer y aceptar habilita el envío del código; ② el campo del código aparece recién cuando se envió.
 */
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
      setCodigoEnviado((await api(`/inversiones/${id}/contrato/codigo`, { cuerpo: { aceptaRiesgos: acepta } })).mensaje)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  async function firmar(e) {
    e.preventDefault()
    setError('')
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
        <section className="paso">
          <h3 className="paso-titulo">① Lee y acepta el contrato</h3>
          <div className="contrato">{contrato.texto}</div>
          <label className="casilla">
            <input type="checkbox" checked={acepta} disabled={Boolean(codigoEnviado)} onChange={(e) => setAcepta(e.target.checked)} />
            Leí y acepto el contrato y los riesgos
          </label>
          {!codigoEnviado && (
            <button type="button" className="boton" onClick={pedirCodigo} disabled={vencida || !acepta}>
              Enviarme el código por correo
            </button>
          )}
          {!acepta && <p className="nota">Marca la casilla para recibir el código con el que firmas.</p>}
        </section>

        {codigoEnviado ? (
          <section className="paso">
            <h3 className="paso-titulo">② Firma con el código</h3>
            <Aviso tipo="ok">{codigoEnviado}</Aviso>
            <Campo etiqueta="Código OTP" ayuda="6 dígitos. Vence a los 10 minutos y tienes 3 intentos.">
              <input
                className="otp"
                inputMode="numeric"
                autoFocus
                maxLength={6}
                placeholder="______"
                value={codigo}
                onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
              />
            </Campo>
            <button className="boton" disabled={vencida || codigo.length !== 6}>
              Firmar y continuar al pago
            </button>
            <button type="button" className="boton-texto" onClick={pedirCodigo} disabled={vencida}>
              ¿No llegó? Enviarme otro código
            </button>
          </section>
        ) : (
          <section className="paso paso-inactivo" aria-hidden>
            <h3 className="paso-titulo">② Firma con el código</h3>
            <p className="nota">Aparece cuando te enviemos el código.</p>
          </section>
        )}
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
