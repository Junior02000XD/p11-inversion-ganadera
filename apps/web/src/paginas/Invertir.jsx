import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, formatoBs, mensajeDe } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'

// GET  /catalogo/:proyectoId → mínimo y cupo libre del proyecto
// POST /inversiones { proyectoId, montoBs } → 201 con la reserva. 400 bajo el mínimo; 409 sin cupo, sin
// identidad verificada o con otra reserva en curso en el mismo proyecto.

/** RF-13: el monto debe superar el mínimo y entrar en el cupo; si cumple, se reserva por 30 minutos (mockup 06a). */
export default function Invertir() {
  const { proyectoId } = useParams()
  const navegar = useNavigate()
  const [proyecto, setProyecto] = useState(null)
  const [monto, setMonto] = useState('')
  const [error, setError] = useState('')
  const [errorMonto, setErrorMonto] = useState(null)
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    api(`/catalogo/${proyectoId}`)
      .then(setProyecto)
      .catch((err) => setError(mensajeDe(err)))
  }, [proyectoId])

  async function reservar(e) {
    e.preventDefault()
    setError('')
    setErrorMonto(null)
    setEnviando(true)
    try {
      const inversion = await api('/inversiones', { cuerpo: { proyectoId, montoBs: Number(monto) } })
      navegar(`/inversiones/${inversion.id}/contrato`)
    } catch (err) {
      // Un 400 trae el error en el campo (con el cupo disponible); un 409, un mensaje general.
      setErrorMonto(err.campos?.MontoBs ?? null)
      if (!err.campos?.MontoBs) setError(mensajeDe(err))
    } finally {
      setEnviando(false)
    }
  }

  if (!proyecto) return <Tarjeta titulo="Invertir">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  return (
    <Tarjeta titulo="Invertir" subtitulo={proyecto.nombre}>
      <form onSubmit={reservar} className="formulario">
        <Campo
          etiqueta="Monto (Bs)"
          ayuda={`Mínimo ${formatoBs(proyecto.minimoBs)} · Cupo disponible ${formatoBs(proyecto.cupoLibreBs)}`}
          error={errorMonto}
        >
          <input type="number" min="1" step="0.01" placeholder="1000" value={monto} onChange={(e) => setMonto(e.target.value)} />
        </Campo>
        <Aviso>{error}</Aviso>
        <button className="boton" disabled={enviando || !monto}>
          Reservar cupo por 30 min
        </button>
        <p className="nota">Mientras dure la reserva, nadie más puede tomar ese cupo. Después firmas el contrato y pagas.</p>
      </form>
    </Tarjeta>
  )
}
