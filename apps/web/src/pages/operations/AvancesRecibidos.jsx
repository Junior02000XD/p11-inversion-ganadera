import { useState } from 'react'
import { Tarjeta } from '../../componentes'
import { expediente } from '../../datosEjemplo'

/** CU «Registrar avance productivo», parte del analista: revisa los avances que cargó el productor (RF-18, RF-19). */
export default function AvancesRecibidos() {
  const [validados, setValidados] = useState({})
  return (
    <Tarjeta titulo="Avances recibidos" subtitulo="Engorde Chiquitania 2026" ancha>
      <ul className="lista">
        {expediente.avances.map((a) => (
          <li key={a.fecha} className="item">
            <div className="item-cabecera">
              <strong>{a.fecha}</strong>
              {validados[a.fecha] && <span className="chip chip-Aprobado">Validado</span>}
            </div>
            <div className="tenue">{a.detalle}</div>
            {!validados[a.fecha] && (
              <button className="boton boton-secundario" onClick={() => setValidados({ ...validados, [a.fecha]: true })}>
                Validar avance
              </button>
            )}
          </li>
        ))}
      </ul>
    </Tarjeta>
  )
}
