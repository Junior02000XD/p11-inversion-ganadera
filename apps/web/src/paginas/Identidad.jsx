import { useEffect, useState } from 'react'
import { api, ErrorApi, formatoFecha, mensajeDe, textoEstado } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { useSesion } from '../sesion'

// GET /verificacion/estado devuelve { estado, historial }, y cada cambio del historial
// es { estadoAnterior, estadoNuevo, fecha, responsable, motivo }.

// `tipo` es el nombre del documento en la API; `campo`, el nombre del campo del formulario multipart.
const fotos = [
  { tipo: 'CiFrente', campo: 'ciFrente', etiqueta: 'CI (anverso)' },
  { tipo: 'CiDorso', campo: 'ciDorso', etiqueta: 'CI (reverso)' },
  { tipo: 'Selfie', campo: 'selfie', etiqueta: 'Selfie' },
]
const etiquetaDe = (tipo) => fotos.find((f) => f.tipo === tipo)?.etiqueta ?? tipo

/** RF-03/04 verificación de identidad con consentimiento previo (RNF-07), mockup 01c. */
export default function Identidad() {
  const { refrescar } = useSesion()
  const [estado, setEstado] = useState(null)
  const [archivos, setArchivos] = useState({})
  const [consiento, setConsiento] = useState(false)
  const [error, setError] = useState('')
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    api('/verificacion/estado').then(setEstado).catch(() => setError('No se pudo cargar tu estado.'))
  }, [])

  async function enviar(e) {
    e.preventDefault()
    setError('')
    if (!consiento) return setError('Necesitamos tu consentimiento para tratar tu CI y tu selfie.')
    const form = new FormData()
    form.append('consentimiento', 'true')
    for (const { campo } of fotos) {
      const archivo = archivos[campo]
      if (archivo) form.append(campo, archivo)
    }
    setEnviando(true)
    try {
      setEstado(await api('/verificacion', { form }))
      await refrescar()
    } catch (err) {
      if (err instanceof ErrorApi && Array.isArray(err.datos.ilegibles)) {
        const lista = err.datos.ilegibles.map(etiquetaDe).join(', ')
        setError(`Imagen ilegible: vuelve a tomar la foto (${lista}).`)
      } else setError(mensajeDe(err))
    } finally {
      setEnviando(false)
    }
  }

  const puedeEnviar = estado && (estado.estado === 'Pendiente' || estado.estado === 'Rechazado')

  return (
    <Tarjeta titulo="Verifica tu identidad" subtitulo="Foto de tu CI (ambas caras) y una selfie. Si la coincidencia es baja, la revisa un analista.">
      {estado && (
        <div className={`estado estado-${estado.estado}`}>
          <span>Estado</span>
          <strong>{textoEstado[estado.estado]}</strong>
        </div>
      )}

      {puedeEnviar && (
        <form onSubmit={enviar} className="formulario">
          {fotos.map(({ campo, etiqueta }) => (
            <Campo key={campo} etiqueta={etiqueta}>
              <div className={`archivo${archivos[campo] ? ' archivo-cargado' : ''}`}>
                <input
                  type="file"
                  accept="image/jpeg,image/png"
                  capture={campo === 'selfie' ? 'user' : 'environment'}
                  required
                  onChange={(e) => setArchivos({ ...archivos, [campo]: e.target.files?.[0] })}
                />
                <span>{archivos[campo]?.name ?? 'Elegir foto (JPG o PNG)'}</span>
              </div>
            </Campo>
          ))}
          <label className="casilla casilla-destacada">
            <input type="checkbox" checked={consiento} onChange={(e) => setConsiento(e.target.checked)} />
            <span>
              Doy mi consentimiento explícito para que la plataforma trate mis datos de identidad y biométricos (CI y selfie) con el único fin de
              verificar mi identidad. Se guardan cifrados.
            </span>
          </label>
          <button className="boton" disabled={enviando || !consiento}>
            {enviando ? 'Enviando…' : 'Enviar para verificación'}
          </button>
          <Aviso>{error}</Aviso>
        </form>
      )}

      {estado && estado.historial.length > 0 && (
        <>
          <h2>Historial</h2>
          <ol className="historial">
            {estado.historial.map((h, i) => (
              <li key={i}>
                <div>
                  <strong>{textoEstado[h.estadoNuevo]}</strong> <span className="tenue">· {formatoFecha(h.fecha)}</span>
                </div>
                <div className="tenue">
                  {h.motivo} — {h.responsable}
                </div>
              </li>
            ))}
          </ol>
        </>
      )}
    </Tarjeta>
  )
}
