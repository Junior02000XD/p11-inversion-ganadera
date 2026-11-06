import { useState } from 'react'
import { useParams } from 'react-router'
import { formatoBs } from '../api'
import { Aviso, Campo, Datos, Tarjeta } from '../componentes'
import { buscarProyecto, liquidacion, portafolio, resultadoLiquidacion } from '../datosEjemplo'

/** RF-24 y RF-25: el inversionista ve su parte y da conformidad con un OTP, o registra un reclamo (mockup 12c). */
export default function MiLiquidacion() {
  const proyecto = buscarProyecto(useParams().id)
  const aporte = portafolio.find((p) => p.proyectoId === proyecto.id)?.montoBs ?? 0
  // La parte de cada inversionista es proporcional a su aporte sobre la meta del proyecto.
  const parte = (resultadoLiquidacion(liquidacion) * aporte) / proyecto.metaBs

  const [codigo, setCodigo] = useState('')
  const [reclamando, setReclamando] = useState(false)
  const [aviso, setAviso] = useState(null)

  function cobrar(e) {
    e.preventDefault()
    if (codigo.length !== 6) return setAviso({ tipo: 'error', texto: 'Ingresa el código de 6 dígitos.' })
    setAviso({ tipo: 'ok', texto: `Transferencia de ${formatoBs(parte)} enviada a tu cuenta.` })
  }

  return (
    <Tarjeta titulo="Tu liquidación" subtitulo={proyecto.nombre}>
      <Datos
        filas={[
          ['Tu aporte', formatoBs(aporte)],
          ['Tu parte', formatoBs(parte)],
        ]}
      />
      <form onSubmit={cobrar} className="formulario">
        <Campo etiqueta="Código OTP">
          <input className="otp" inputMode="numeric" maxLength={6} placeholder="______" value={codigo} onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))} />
        </Campo>
        <button className="boton">Estoy conforme, cobrar</button>
        <button type="button" className="boton boton-secundario" onClick={() => setReclamando(true)}>
          Registrar reclamo
        </button>
      </form>

      {reclamando && (
        <form
          className="formulario panel"
          onSubmit={(e) => {
            e.preventDefault()
            setReclamando(false)
            setAviso({ tipo: 'ok', texto: 'Reclamo registrado. Un analista revisará la liquidación.' })
          }}
        >
          <Campo etiqueta="Motivo del reclamo">
            <textarea required placeholder="Ej. no coinciden los costos declarados" />
          </Campo>
          <button className="boton">Enviar reclamo</button>
        </form>
      )}
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
    </Tarjeta>
  )
}
