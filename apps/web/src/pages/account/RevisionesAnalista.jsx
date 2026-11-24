import { useCallback, useEffect, useState } from 'react'
import { abrirArchivo, api, formatoFecha, mensajeDe } from '../../api'
import { Aviso, Tarjeta } from '../../componentes'

// Verificación pendiente: { usuarioId, nombre, correo, ci, rol, consentimientoEn, documentos: [{ id, tipo, nombreOriginal }] }
// Documento del predio en revisión: { id, correo, tipo, nombreOriginal, subidoEn }

const nombres = {
  CiFrente: 'CI anverso',
  CiDorso: 'CI reverso',
  Selfie: 'selfie',
  Runsa: 'RUNSA',
  TituloPropiedad: 'Título del predio',
  ContratoAlquiler: 'Contrato de alquiler',
}

/** Estado y acciones comunes de las dos bandejas de revisión del analista. */
function useRevision(ruta) {
  const [items, setItems] = useState([])
  const [motivos, setMotivos] = useState({})
  const [aviso, setAviso] = useState(null)

  const cargar = useCallback(
    () =>
      api(ruta)
        .then(setItems)
        .catch((err) => setAviso({ tipo: 'error', texto: mensajeDe(err) })),
    [ruta],
  )
  useEffect(() => {
    cargar()
  }, [cargar])

  async function decidir(rutaDecision, id, aprobar) {
    setAviso(null)
    try {
      const r = await api(rutaDecision, {
        cuerpo: { aprobar, motivo: motivos[id] ?? '' },
      })
      setAviso({ tipo: 'ok', texto: r.mensaje })
      await cargar()
    } catch (err) {
      setAviso({ tipo: 'error', texto: mensajeDe(err) })
    }
  }

  // RNF-02: el archivo llega descifrado desde la API; en disco está cifrado.
  const ver = (id) => abrirArchivo(`/analista/archivos/${id}`).catch((err) => setAviso({ tipo: 'error', texto: mensajeDe(err) }))

  const motivo = (id) => (
    <input
      className="motivo"
      placeholder="Motivo (obligatorio para rechazar)"
      value={motivos[id] ?? ''}
      onChange={(e) => setMotivos({ ...motivos, [id]: e.target.value })}
    />
  )
  const botones = (rutaDecision, id) => (
    <div className="acciones">
      <button className="boton" onClick={() => decidir(rutaDecision, id, true)}>
        Aprobar
      </button>
      <button className="boton boton-peligro" onClick={() => decidir(rutaDecision, id, false)}>
        Rechazar
      </button>
    </div>
  )
  return { items, aviso, ver, motivo, botones }
}

/** CU «Registrar cuenta y verificar identidad», parte del analista: revisión manual (RF-03/04). */
export function RevisionIdentidad() {
  const { items: verifs, aviso, ver, motivo, botones } = useRevision('/analista/verificaciones')
  return (
    <Tarjeta titulo="Identidad en revisión manual" subtitulo="Cuentas cuyo puntaje de verificación quedó bajo el umbral." ancha>
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
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
    </Tarjeta>
  )
}

/** «Verificar productor y predio» (RF-05), incluido en «Evaluar y publicar proyecto». */
export function RevisionPredio() {
  const { items: docs, aviso, ver, motivo, botones } = useRevision('/analista/documentos')
  return (
    <Tarjeta titulo="Documentos del predio por revisar" subtitulo="Sin RUNSA y título o contrato aprobados, el proyecto no pasa a evaluación técnica." ancha>
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
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
