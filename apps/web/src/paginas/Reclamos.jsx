import { useState } from 'react'
import { Link } from 'react-router'
import { formatoBs } from '../api'
import { Archivo, Aviso, Tarjeta } from '../componentes'
import { buscarProyecto, reclamos } from '../datosEjemplo'

/** CU «Gestionar reclamo al seguro» (RF-21): extiende «Gestionar incidencias» cuando la pérdida está asegurada. */
export default function Reclamos() {
  const [enviados, setEnviados] = useState({})
  return (
    <Tarjeta titulo="Reclamos al seguro" subtitulo="Nacen de una incidencia con pérdida asegurada." ancha>
      <ul className="lista">
        {reclamos.map((r) => (
          <li key={r.id} className="item">
            <div className="item-cabecera">
              <strong>
                {r.id} · {buscarProyecto(r.proyectoId).nombre}
              </strong>
              <span className="chip">{enviados[r.id] ? 'Enviado a la aseguradora' : r.estado}</span>
            </div>
            <div className="tenue">
              {r.aseguradora} · {formatoBs(r.montoBs)} · <Link to={`/incidencias/${r.incidenciaId}`}>incidencia #{r.incidenciaId}</Link>
            </div>
            {enviados[r.id] ? (
              <Aviso tipo="ok">Reclamo enviado con el informe veterinario. Se avisa al productor y a los inversionistas.</Aviso>
            ) : (
              <div className="formulario">
                <Archivo texto="Adjuntar informe veterinario" alElegir={(f) => f && setEnviados({ ...enviados, [r.id]: true })} />
              </div>
            )}
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
