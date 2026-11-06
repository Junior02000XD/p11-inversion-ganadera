import { useEffect, useState } from 'react'
import { abrirArchivo, api, formatoFecha, mensajeDe } from '../api'
import { Aviso, Tarjeta } from '../componentes'

// Verificación pendiente: { usuarioId, nombre, correo, ci, rol, consentimientoEn, documentos: [{ id, tipo, nombreOriginal }] }
// Documento del predio en revisión: { id, correo, tipo, nombreOriginal, subidoEn }

const nombres = {
  CiFrente: 'CI anverso', CiDorso: 'CI reverso', Selfie: 'selfie',
  Runsa: 'RUNSA', TituloPropiedad: 'Título del predio', ContratoAlquiler: 'Contrato de alquiler',
}

/** Revisión manual de identidad (RF-03/04) y de documentos del predio (RF-05). */
export default function PanelAnalista() {
  const [verifs, setVerifs] = useState([])
  const [docs, setDocs] = useState([])
  const [motivos, setMotivos] = useState({})
  const [aviso, setAviso] = useState(null)

  const cargar = async () => {
    setVerifs(await api('/analista/verificaciones'))
    setDocs(await api('/analista/documentos'))
  }
  useEffect(() => {
    cargar()
  }, [])

  async function decidir(ruta, id, aprobar) {
    setAviso(null)
    try {
      const r = await api(ruta, { cuerpo: { aprobar, motivo: motivos[id] ?? '' } })
      setAviso({ tipo: 'ok', texto: r.mensaje })
      await cargar()
    } catch (err) {
      setAviso({ tipo: 'error', texto: mensajeDe(err) })
    }
  }

  // RNF-02: el archivo llega descifrado desde la API; en disco está cifrado.
  const ver = (id) => abrirArchivo(`/analista/archivos/${id}`).catch((err) => setAviso({ tipo: 'error', texto: mensajeDe(err) }))

  const motivo = (id) => (
    <input className="motivo" placeholder="Motivo (obligatorio para rechazar)" value={motivos[id] ?? ''} onChange={(e) => setMotivos({ ...motivos, [id]: e.target.value })} />
  )
  const botones = (ruta, id) => (
    <div className="acciones">
      <button className="boton" onClick={() => decidir(ruta, id, true)}>
        Aprobar
      </button>
      <button className="boton boton-peligro" onClick={() => decidir(ruta, id, false)}>
        Rechazar
      </button>
    </div>
  )

  return (
    <Tarjeta titulo="Revisiones pendientes" ancha>
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}

      <h2>Identidad en revisión manual ({verifs.length})</h2>
      {verifs.length === 0 && <p className="tenue">No hay verificaciones pendientes.</p>}
      <ul className="lista">
        {verifs.map((v) => (
          <li key={v.usuarioId} className="item">
            <div className="item-cabecera">
              <strong>{v.nombre}</strong>
              <span className="chip">{v.rol}</span>
            </div>
            <div className="tenue">
              {v.correo} · CI {v.ci} · consintió {v.consentimientoEn ? formatoFecha(v.consentimientoEn) : '—'}
            </div>
            <div className="enlaces">
              {v.documentos.map((d) => (
                <button key={d.id} className="boton-texto" onClick={() => ver(d.id)}>
                  Ver {nombres[d.tipo] ?? d.tipo}
                </button>
              ))}
            </div>
            {motivo(v.usuarioId)}
            {botones(`/analista/verificaciones/${v.usuarioId}/decision`, v.usuarioId)}
          </li>
        ))}
      </ul>

      <h2>Documentos del predio ({docs.length})</h2>
      {docs.length === 0 && <p className="tenue">No hay documentos pendientes.</p>}
      <ul className="lista">
        {docs.map((d) => (
          <li key={d.id} className="item">
            <div className="item-cabecera">
              <strong>{nombres[d.tipo] ?? d.tipo}</strong>
              <span className="tenue">{formatoFecha(d.subidoEn)}</span>
            </div>
            <div className="tenue">
              {d.correo} ·{' '}
              <button className="boton-texto" onClick={() => ver(d.id)}>
                {d.nombreOriginal}
              </button>
            </div>
            {motivo(d.id)}
            {botones(`/analista/documentos/${d.id}/decision`, d.id)}
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
