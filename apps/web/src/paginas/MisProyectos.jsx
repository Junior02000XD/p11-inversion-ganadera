import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoProyecto } from '../api'
import { Aviso, Tarjeta } from '../componentes'

// Cada proyecto llega como { id, linea, nombre, ubicacion, montoRequeridoBs, plazoMeses,
// estado, creadoEn, cantidadAnimales, vacasEnOrdeno, observaciones }. linea: 'Engorde' | 'Lecheria'.
// observaciones: lo último que escribió el analista al observar o rechazar (RF-09).

export default function MisProyectos() {
  const location = useLocation()
  const [proyectos, setProyectos] = useState(null)
  const [mensajes, setMensajes] = useState({})

  const cargar = () => api('/proyectos').then(setProyectos)
  useEffect(() => {
    cargar()
  }, [])

  async function enviarARevision(id) {
    try {
      await api(`/proyectos/${id}/enviar-revision`, { metodo: 'POST' })
      setMensajes({ ...mensajes, [id]: { tipo: 'ok', texto: 'Enviado a evaluación técnica.' } })
      await cargar()
    } catch (err) {
      setMensajes({ ...mensajes, [id]: { tipo: 'error', texto: mensajeDe(err) } })
    }
  }

  // NuevoProyecto nos manda el nombre del proyecto recién creado para mostrar el aviso.
  const creado = location.state?.creado

  return (
    <Tarjeta titulo="Mis proyectos" ancha>
      {creado && <Aviso tipo="ok">Guardamos «{creado}» como borrador.</Aviso>}
      <Link className="boton" to="/proyectos/nuevo">
        Nuevo proyecto
      </Link>
      {proyectos?.length === 0 && <p className="tenue">Todavía no tienes proyectos.</p>}
      <ul className="lista">
        {proyectos?.map((p) => (
          <li key={p.id} className="item">
            <div className="item-cabecera">
              <strong>{p.nombre}</strong>
              <span className={`chip chip-${p.estado}`}>{textoEstadoProyecto[p.estado] ?? p.estado}</span>
            </div>
            <div className="tenue">
              {p.linea === 'Lecheria' ? `Lechería · ${p.vacasEnOrdeno} vacas en ordeño` : `Engorde · ${p.cantidadAnimales} cabezas`} · {p.ubicacion}
            </div>
            <div className="tenue">
              {formatoBs(p.montoRequeridoBs)} · {p.plazoMeses} meses · creado {formatoFecha(p.creadoEn)}
            </div>
            {p.observaciones && ['Borrador', 'Rechazado'].includes(p.estado) && (
              <Aviso tipo={p.estado === 'Rechazado' ? 'error' : 'info'}>
                {p.estado === 'Rechazado' ? 'Motivo del rechazo' : 'Observaciones del analista'}: {p.observaciones}
              </Aviso>
            )}
            {p.estado === 'Borrador' && (
              <button className="boton boton-secundario" onClick={() => enviarARevision(p.id)}>
                Enviar a evaluación técnica
              </button>
            )}
            {mensajes[p.id] && <Aviso tipo={mensajes[p.id].tipo}>{mensajes[p.id].texto}</Aviso>}
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
