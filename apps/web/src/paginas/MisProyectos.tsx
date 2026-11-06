import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe } from '../api'
import { Aviso, Tarjeta } from '../componentes'

interface Proyecto {
  id: string
  linea: 'Engorde' | 'Lecheria'
  nombre: string
  ubicacion: string
  montoRequeridoBs: number
  plazoMeses: number
  estado: 'Borrador' | 'EnRevision'
  creadoEn: string
  cantidadAnimales: number | null
  vacasEnOrdeno: number | null
}

export default function MisProyectos() {
  const location = useLocation()
  const [proyectos, setProyectos] = useState<Proyecto[] | null>(null)
  const [mensajes, setMensajes] = useState<Record<string, { tipo: 'ok' | 'error'; texto: string }>>({})

  const cargar = () => api<Proyecto[]>('/proyectos').then(setProyectos)
  useEffect(() => {
    void cargar()
  }, [])

  async function enviarARevision(id: string) {
    try {
      await api(`/proyectos/${id}/enviar-revision`, { metodo: 'POST' })
      setMensajes({ ...mensajes, [id]: { tipo: 'ok', texto: 'Enviado a evaluación técnica.' } })
      await cargar()
    } catch (err) {
      setMensajes({ ...mensajes, [id]: { tipo: 'error', texto: mensajeDe(err) } })
    }
  }

  const creado = (location.state as { creado?: string } | null)?.creado

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
              <span className={`chip chip-${p.estado}`}>{p.estado === 'Borrador' ? 'Borrador' : 'En revisión'}</span>
            </div>
            <div className="tenue">
              {p.linea === 'Lecheria' ? `Lechería · ${p.vacasEnOrdeno} vacas en ordeño` : `Engorde · ${p.cantidadAnimales} cabezas`} · {p.ubicacion}
            </div>
            <div className="tenue">
              {formatoBs(p.montoRequeridoBs)} · {p.plazoMeses} meses · creado {formatoFecha(p.creadoEn)}
            </div>
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
