import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoEstadoProyecto } from '../api'
import { Aviso, Tarjeta } from '../componentes'
import { ADMINISTRADOR } from '../roles'
import { useSesion } from '../sesion'

// Cada proyecto llega como { id, linea, nombre, ubicacion, montoRequeridoBs, plazoMeses,
// estado, creadoEn, cantidadAnimales, vacasEnOrdeno, observaciones }. linea: 'Engorde' | 'Lecheria'.
// observaciones: lo último que escribió el analista al observar o rechazar (RF-09).

/** CU «Postular proyecto» (RF-07, RF-08): fichas en borrador, documentos del predio y envío a evaluación. */
export default function MisProyectos() {
  const location = useLocation()
  const { usuario } = useSesion()
  const esAdmin = usuario?.rol === ADMINISTRADOR
  const [proyectos, setProyectos] = useState(null)
  const [mensajes, setMensajes] = useState({})

  const cargar = () => api('/proyectos').then(setProyectos)
  useEffect(() => {
    cargar()
  }, [])

  async function enviarARevision(id) {
    try {
      await api(`/proyectos/${id}/enviar-revision`, { metodo: 'POST' })
      setMensajes({
        ...mensajes,
        [id]: { tipo: 'ok', texto: 'Enviado a evaluación técnica.' },
      })
      await cargar()
    } catch (err) {
      setMensajes({
        ...mensajes,
        [id]: { tipo: 'error', texto: mensajeDe(err) },
      })
    }
  }

  async function eliminar(p) {
    if (!window.confirm(`¿Eliminar el borrador «${p.nombre}»?`)) return
    try {
      await api(`/proyectos/${p.id}`, { metodo: 'DELETE' })
      await cargar()
    } catch (err) {
      setMensajes({
        ...mensajes,
        [p.id]: { tipo: 'error', texto: mensajeDe(err) },
      })
    }
  }

  // NuevoProyecto nos manda el nombre del proyecto recién creado para mostrar el aviso.
  const creado = location.state?.creado

  return (
    <Tarjeta
      titulo={esAdmin ? 'Proyectos postulados' : 'Mis proyectos'}
      subtitulo={esAdmin ? 'Todos los productores. Puedes postular en nombre de uno.' : undefined}
      ancha
    >
      {creado && <Aviso tipo="ok">Guardamos «{creado}» como borrador.</Aviso>}
      <div className="acciones">
        <Link className="boton" to="/proyectos/nuevo">
          Nuevo proyecto
        </Link>
        <Link className="boton boton-secundario" to="/proyectos/documentos">
          Documentos del predio
        </Link>
      </div>
      {proyectos?.length === 0 && <p className="tenue">{esAdmin ? 'Ningún productor postuló proyectos todavía.' : 'Todavía no tienes proyectos.'}</p>}
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
              <div className="acciones">
                <button className="boton boton-secundario" onClick={() => enviarARevision(p.id)}>
                  Enviar a evaluación técnica
                </button>
                <Link className="boton boton-secundario" to={`/proyectos/${p.id}/editar`}>
                  Editar
                </Link>
                <button className="boton boton-peligro" onClick={() => eliminar(p)}>
                  Eliminar
                </button>
              </div>
            )}
            {mensajes[p.id] && <Aviso tipo={mensajes[p.id].tipo}>{mensajes[p.id].texto}</Aviso>}
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
