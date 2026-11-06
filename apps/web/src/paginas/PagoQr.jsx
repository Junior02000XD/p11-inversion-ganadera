import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router'
import { formatoBs } from '../api'
import { Aviso, Progreso, Tarjeta } from '../componentes'
import { buscarProyecto } from '../datosEjemplo'

const RESERVA_SEGUNDOS = 30 * 60

/** RF-15: pago por QR mientras dura la reserva del cupo; si vence, el cupo se libera (mockup 06c). */
export default function PagoQr() {
  const proyecto = buscarProyecto(useParams().id)
  const monto = useLocation().state?.monto ?? 1000
  const navegar = useNavigate()
  const [restantes, setRestantes] = useState(RESERVA_SEGUNDOS)

  // Cuenta regresiva de la reserva, un segundo a la vez.
  useEffect(() => {
    const reloj = setInterval(() => setRestantes((s) => Math.max(s - 1, 0)), 1000)
    return () => clearInterval(reloj)
  }, [])

  const minutos = String(Math.floor(restantes / 60)).padStart(2, '0')
  const segundos = String(restantes % 60).padStart(2, '0')
  const vencida = restantes === 0

  return (
    <Tarjeta titulo="Pagar con QR" subtitulo={`${proyecto.nombre} · Monto: ${formatoBs(monto)}`}>
      <div className="formulario">
        <div className="qr" role="img" aria-label="Código QR de pago" />
        <span className="tenue">Escanéalo desde la app de tu banco. Tiempo restante de la reserva: {minutos}:{segundos}</span>
        <Progreso porcentaje={(restantes / RESERVA_SEGUNDOS) * 100} />
        {vencida ? (
          <Aviso>Pago rechazado o vencido: se libera tu cupo.</Aviso>
        ) : (
          <button className="boton" onClick={() => navegar(`/catalogo/${proyecto.id}/ronda`, { state: { monto } })}>
            Ya pagué, verificar
          </button>
        )}
        <p className="nota">Con el pago confirmado se emite tu constancia de participación.</p>
      </div>
    </Tarjeta>
  )
}
