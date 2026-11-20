import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { api, formatoBs, mensajeDe } from '../api'
import { Aviso, CuentaRegresiva, Tarjeta } from '../componentes'

// POST /inversiones/:id/pago            → { referencia, montoBs, contenidoQr, venceEn } (409 sin contrato firmado)
// POST /inversiones/:id/pago/confirmar  { aprobado } → simula el aviso de la pasarela de pago

/** RF-15: pago por QR (simulado) mientras dura la reserva; si se rechaza o vence, el cupo se libera (mockup 06c). */
export default function PagoQr() {
  const { id } = useParams()
  const navegar = useNavigate()
  const [pago, setPago] = useState(null)
  const [vencida, setVencida] = useState(false)
  const [rechazado, setRechazado] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    api(`/inversiones/${id}/pago`, { metodo: 'POST' })
      .then(setPago)
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  async function confirmar(aprobado) {
    setError('')
    try {
      await api(`/inversiones/${id}/pago/confirmar`, { cuerpo: { aprobado } })
      if (aprobado) navegar(`/portafolio/${id}`, { state: { recien: true } })
      else setRechazado(true)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  if (!pago) return <Tarjeta titulo="Pagar con QR">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  const cerrado = vencida || rechazado
  return (
    <Tarjeta titulo="Pagar con QR" subtitulo={`Monto: ${formatoBs(pago.montoBs)} · Referencia ${pago.referencia}`}>
      <div className="formulario">
        <div className="qr" role="img" aria-label={`Código QR de pago ${pago.referencia}`} title={pago.contenidoQr} />
        <span className="tenue">Escanéalo desde la app de tu banco (pago simulado).</span>
        <CuentaRegresiva hasta={pago.venceEn} alVencer={() => setVencida(true)} />
        {vencida && <Aviso>La reserva venció: se liberó tu cupo y no se cobró nada.</Aviso>}
        {rechazado && <Aviso>El banco rechazó el pago: se liberó tu cupo. Te avisamos por correo.</Aviso>}
        {!cerrado && (
          <>
            <button className="boton" onClick={() => confirmar(true)}>
              Ya pagué, verificar
            </button>
            <button className="boton boton-secundario" onClick={() => confirmar(false)}>
              Simular pago rechazado
            </button>
          </>
        )}
        {cerrado && (
          <Link className="boton boton-secundario" to="/catalogo">
            Volver al catálogo
          </Link>
        )}
        <Aviso>{error}</Aviso>
        <p className="nota">Con el pago confirmado se asigna tu participación y se emite tu constancia.</p>
      </div>
    </Tarjeta>
  )
}
