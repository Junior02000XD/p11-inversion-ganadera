import { Link } from 'react-router'
import { Tarjeta } from '../../componentes'
import { proyectos } from '../../datosEjemplo'

/** CU «Consultar expediente del proyecto» (RF-22): elegir el proyecto cuyo expediente se abre. */
export default function Expedientes() {
  return (
    <Tarjeta titulo="Expedientes" subtitulo="Todo lo que pasó en cada proyecto: avances, documentos e incidencias." ancha>
      <ul className="lista">
        {proyectos.map((p) => (
          <li key={p.id} className="item">
            <div className="item-cabecera">
              <strong>{p.nombre}</strong>
              <span className="tenue">{p.linea === 'Lecheria' ? 'Lechería' : 'Engorde'}</span>
            </div>
            <div className="tenue">{p.ubicacion}</div>
            <Link to={`/expedientes/${p.id}`}>Abrir expediente</Link>
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
