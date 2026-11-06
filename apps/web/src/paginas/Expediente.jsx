import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Datos, Tarjeta } from '../componentes'
import { buscarProyecto, expediente } from '../datosEjemplo'

const pestanas = ['Resumen', 'Avances', 'Documentos']

/** RF-22: todo lo que pasó en el proyecto, en un solo lugar (mockup 11b). */
export default function Expediente() {
  const proyecto = buscarProyecto(useParams().id)
  const [pestana, setPestana] = useState('Resumen')
  const ultimo = expediente.avances[0]

  return (
    <Tarjeta titulo="Expediente digital" subtitulo={proyecto.nombre} ancha>
      <div className="segmentado segmentado-3" role="tablist">
        {pestanas.map((p) => (
          <button key={p} type="button" role="tab" aria-selected={pestana === p} className={pestana === p ? 'activo' : ''} onClick={() => setPestana(p)}>
            {p}
          </button>
        ))}
      </div>

      {pestana === 'Resumen' && (
        <Datos
          filas={[
            ['Estado', expediente.estado],
            ['Último avance', `${ultimo.fecha} · ${ultimo.detalle}`],
            ['Incidencias', `${expediente.incidenciasAbiertas} abierta`],
          ]}
        />
      )}
      {pestana === 'Avances' && (
        <ol className="historial">
          {expediente.avances.map((a) => (
            <li key={a.fecha}>
              <strong>{a.fecha}</strong> <span className="tenue">· {a.detalle}</span>
            </li>
          ))}
        </ol>
      )}
      {pestana === 'Documentos' && (
        <ul className="lista lista-compacta">
          {expediente.documentos.map((d) => (
            <li key={d}>{d}</li>
          ))}
        </ul>
      )}

      <div className="acciones">
        <button className="boton boton-secundario">Descargar contrato</button>
        <Link className="boton" to={`/portafolio/${proyecto.id}/liquidacion`}>
          Ver liquidación
        </Link>
      </div>
    </Tarjeta>
  )
}
