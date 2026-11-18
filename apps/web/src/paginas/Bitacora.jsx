import { useEffect, useState } from 'react'
import { api, formatoFecha, mensajeDe } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'

// GET /admin/bitacora → [{ fecha, modulo, referenciaId, referencia, accion, estadoAnterior, estadoNuevo, responsable, motivo }]

/** CU «Consultar bitácora de auditoría» (RNF-03): quién cambió qué estado, cuándo y por qué. */
export default function Bitacora() {
  const [eventos, setEventos] = useState(null)
  const [responsable, setResponsable] = useState('')
  const [error, setError] = useState('')

  async function cargar(filtro = '') {
    setError('')
    try {
      const q = filtro.trim() ? `?responsable=${encodeURIComponent(filtro.trim())}` : ''
      setEventos(await api(`/admin/bitacora${q}`))
    } catch (err) {
      setError(mensajeDe(err))
    }
  }
  useEffect(() => {
    cargar()
  }, [])

  return (
    <Tarjeta titulo="Bitácora de auditoría" subtitulo="Cambios de estado de identidades y proyectos, del más reciente al más antiguo." ancha>
      <form
        className="fila-filtro"
        onSubmit={(e) => {
          e.preventDefault()
          cargar(responsable)
        }}
      >
        <Campo etiqueta="Responsable (correo)">
          <input placeholder="analista@p11.local" value={responsable} onChange={(e) => setResponsable(e.target.value)} />
        </Campo>
        <button className="boton boton-secundario">Filtrar</button>
      </form>
      <Aviso>{error}</Aviso>
      {eventos?.length === 0 && <p className="tenue">No hay eventos registrados.</p>}
      {eventos?.length > 0 && (
        <div className="tabla-scroll">
          <table className="tabla">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Módulo</th>
                <th>Sobre</th>
                <th>Cambio</th>
                <th>Responsable</th>
              </tr>
            </thead>
            <tbody>
              {eventos.map((e, i) => (
                <tr key={`${e.referenciaId}-${e.fecha}-${i}`}>
                  <td>{formatoFecha(e.fecha)}</td>
                  <td>{e.modulo}</td>
                  <td>
                    <strong>{e.referencia}</strong>
                    <div className="tenue">{e.accion}</div>
                  </td>
                  <td>
                    {e.estadoAnterior} → {e.estadoNuevo}
                    {e.motivo && <div className="tenue">{e.motivo}</div>}
                  </td>
                  <td>{e.responsable}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Tarjeta>
  )
}
