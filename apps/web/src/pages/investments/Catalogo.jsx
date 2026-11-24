import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, formatoBs, formatoFecha, mensajeDe, textoLinea } from '../../api'
import { Aviso, Campo, Progreso, Tarjeta } from '../../componentes'

// GET /catalogo?linea=&plazoMaximo=&minimoHasta=&zona=&riesgo= → proyectos publicados:
// [{ id, nombre, linea, ubicacion, plazoMeses, metaBs, minimoBs, recaudadoBs, cupoLibreBs, porcentajeFinanciado, nivelRiesgo, cierreRondaEn, ... }]

const sinFiltros = { linea: '', plazoMaximo: '', minimoHasta: '', zona: '', riesgo: '' }

/** Arma «?linea=Engorde&zona=Warnes» solo con los filtros que tienen valor. */
const consulta = (filtros) => {
  const params = new URLSearchParams(Object.entries(filtros).filter(([, valor]) => valor !== ''))
  return params.size ? `?${params}` : ''
}

/** RF-11: proyectos publicados y abiertos a inversión, con filtros por línea, plazo, monto mínimo, zona y riesgo (mockup 05a). */
export default function Catalogo() {
  const [filtros, setFiltros] = useState(sinFiltros)
  const [proyectos, setProyectos] = useState(null)
  const [error, setError] = useState('')

  // Cada vez que cambia un filtro se vuelve a pedir el catálogo; el filtrado lo hace la API.
  useEffect(() => {
    api(`/catalogo${consulta(filtros)}`)
      .then(setProyectos)
      .catch((err) => setError(mensajeDe(err)))
  }, [filtros])

  const cambiar = (clave) => (e) => setFiltros({ ...filtros, [clave]: e.target.value })

  return (
    <Tarjeta titulo="Proyectos abiertos" subtitulo="Proyectos evaluados y publicados por el equipo técnico." ancha>
      <div className="filtros">
        <Campo etiqueta="Línea">
          <select value={filtros.linea} onChange={cambiar('linea')}>
            <option value="">Todas</option>
            <option value="Engorde">Engorde</option>
            <option value="Lecheria">Lechería</option>
          </select>
        </Campo>
        <Campo etiqueta="Plazo máximo (meses)">
          <input type="number" min="1" placeholder="12" value={filtros.plazoMaximo} onChange={cambiar('plazoMaximo')} />
        </Campo>
        <Campo etiqueta="Monto mínimo hasta (Bs)">
          <input type="number" min="1" placeholder="500" value={filtros.minimoHasta} onChange={cambiar('minimoHasta')} />
        </Campo>
        <Campo etiqueta="Zona">
          <input placeholder="Warnes" value={filtros.zona} onChange={cambiar('zona')} />
        </Campo>
        <Campo etiqueta="Riesgo">
          <select value={filtros.riesgo} onChange={cambiar('riesgo')}>
            <option value="">Todos</option>
            <option value="Bajo">Bajo</option>
            <option value="Medio">Medio</option>
            <option value="Alto">Alto</option>
          </select>
        </Campo>
      </div>

      <Aviso>{error}</Aviso>
      {proyectos?.length === 0 && <p className="tenue">Sin proyectos que coincidan con el filtro.</p>}
      <ul className="lista">
        {proyectos?.map((p) => (
          <li key={p.id} className="item">
            <div className="item-cabecera">
              <strong>{p.nombre}</strong>
              <span className={`chip chip-${p.nivelRiesgo}`}>Riesgo {p.nivelRiesgo?.toLowerCase()}</span>
            </div>
            <div className="tenue">
              {textoLinea(p.linea)} · {p.ubicacion} · {p.plazoMeses} meses · mínimo {formatoBs(p.minimoBs)}
            </div>
            <Progreso porcentaje={p.porcentajeFinanciado} />
            <div className="tenue">
              {formatoBs(p.recaudadoBs)} de {formatoBs(p.metaBs)} · Cupo libre {formatoBs(p.cupoLibreBs)} · Cierra {formatoFecha(p.cierreRondaEn)}
            </div>
            <Link className="boton boton-secundario" to={`/catalogo/${p.id}`}>
              Ver y simular
            </Link>
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
