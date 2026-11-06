import { useState } from 'react'
import { Link } from 'react-router'
import { formatoBs } from '../api'
import { Campo, Progreso, Tarjeta } from '../componentes'
import { proyectos } from '../datosEjemplo'

/** RF-11: proyectos publicados y abiertos a inversión, con filtro por línea (mockup 05a). */
export default function Catalogo() {
  const [linea, setLinea] = useState('Todas')
  const visibles = linea === 'Todas' ? proyectos : proyectos.filter((p) => p.linea === linea)

  return (
    <Tarjeta titulo="Proyectos abiertos" subtitulo="Proyectos evaluados y publicados por el equipo técnico." ancha>
      <Campo etiqueta="Filtrar por línea">
        <select value={linea} onChange={(e) => setLinea(e.target.value)}>
          <option value="Todas">Todas</option>
          <option value="Engorde">Engorde</option>
          <option value="Lecheria">Lechería</option>
        </select>
      </Campo>

      {visibles.length === 0 && <p className="tenue">Sin proyectos que coincidan con el filtro.</p>}
      <ul className="lista">
        {visibles.map((p) => {
          const cupoLibre = 100 - (p.recaudadoBs / p.metaBs) * 100
          return (
            <li key={p.id} className="item">
              <div className="item-cabecera">
                <strong>{p.nombre}</strong>
                <span className="chip chip-Pendiente">Riesgo {p.riesgo.toLowerCase()}</span>
              </div>
              <div className="tenue">
                {p.linea === 'Lecheria' ? 'Lechería' : 'Engorde'} · {p.ubicacion} · {p.plazoMeses} meses
              </div>
              <Progreso porcentaje={100 - cupoLibre} />
              <div className="tenue">
                Meta {formatoBs(p.metaBs)} · Cupo libre {Math.round(cupoLibre)} % · Cierra {p.cierre}
              </div>
              <Link className="boton boton-secundario" to={`/catalogo/${p.id}`}>
                Ver y simular
              </Link>
            </li>
          )
        })}
      </ul>
    </Tarjeta>
  )
}
