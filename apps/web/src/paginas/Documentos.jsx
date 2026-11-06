import { useEffect, useState } from 'react'
import { api, formatoFecha, mensajeDe } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'

// Cada documento llega como { id, tipo, nombreOriginal, estado, motivo, subidoEn }.
// estado: 'Pendiente' | 'Aprobado' | 'Rechazado'

const tipos = { Runsa: 'Registro RUNSA', TituloPropiedad: 'Título del predio', ContratoAlquiler: 'Contrato de alquiler' }

/** RF-05: documentos del productor y del predio (mockup 03b). */
export default function Documentos() {
  const [docs, setDocs] = useState([])
  const [tipo, setTipo] = useState('Runsa')
  const [archivo, setArchivo] = useState(null)
  const [error, setError] = useState('')
  const [ok, setOk] = useState('')

  const cargar = () => api('/productor/documentos').then(setDocs)
  useEffect(() => {
    cargar()
  }, [])

  async function subir(e) {
    e.preventDefault()
    setError('')
    setOk('')
    if (!archivo) return
    const form = new FormData()
    form.append('tipo', tipo)
    form.append('archivo', archivo)
    try {
      await api('/productor/documentos', { form })
      setOk(`${tipos[tipo]} cargado. Queda pendiente de revisión.`)
      setArchivo(null)
      e.target.reset()
      await cargar()
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  const ultimo = (t) => docs.find((d) => d.tipo === t)

  return (
    <Tarjeta titulo="Documentos del predio" subtitulo="Sin RUNSA y título o contrato aprobados, el proyecto no pasa a evaluación técnica.">
      <table className="tabla">
        <tbody>
          {Object.keys(tipos).map((t) => {
            const d = ultimo(t)
            return (
              <tr key={t}>
                <td>{tipos[t]}</td>
                <td>
                  <span className={`chip chip-${d?.estado ?? 'Falta'}`}>{d ? d.estado : 'Falta'}</span>
                  {d?.motivo && <div className="tenue">{d.motivo}</div>}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>

      <form onSubmit={subir} className="formulario">
        <Campo etiqueta="Tipo de documento">
          <select value={tipo} onChange={(e) => setTipo(e.target.value)}>
            {Object.entries(tipos).map(([k, v]) => (
              <option key={k} value={k}>
                {v}
              </option>
            ))}
          </select>
        </Campo>
        <Campo etiqueta="Archivo" ayuda="PDF, JPG o PNG, hasta 5 MB. Se guarda cifrado.">
          <div className={`archivo${archivo ? ' archivo-cargado' : ''}`}>
            <input type="file" accept="application/pdf,image/jpeg,image/png" required onChange={(e) => setArchivo(e.target.files?.[0] ?? null)} />
            <span>{archivo?.name ?? 'Elegir archivo'}</span>
          </div>
        </Campo>
        <button className="boton" disabled={!archivo}>
          Subir documento
        </button>
        <Aviso>{error}</Aviso>
        <Aviso tipo="ok">{ok}</Aviso>
      </form>

      {docs.length > 0 && (
        <>
          <h2>Historial de cargas</h2>
          <ul className="lista lista-compacta">
            {docs.map((d) => (
              <li key={d.id}>
                {tipos[d.tipo]} · {d.nombreOriginal} <span className="tenue">· {formatoFecha(d.subidoEn)} · {d.estado}</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </Tarjeta>
  )
}
