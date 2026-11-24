import { useState } from 'react'
import { Link, useLocation } from 'react-router'
import { Aviso, Tarjeta } from '../../componentes'
import { avancesGuardados } from '../../datosEjemplo'

/** RNF-04: avances guardados sin conexión, que se suben al recuperar señal (mockup 08b). */
export default function AvancesPendientes() {
  const nuevo = useLocation().state?.nuevo
  const [avances, setAvances] = useState(nuevo ? [{ fecha: 'Hoy', estado: 'Pendiente' }, ...avancesGuardados] : avancesGuardados)
  const [enviados, setEnviados] = useState(false)
  const hayPendientes = avances.some((a) => a.estado === 'Pendiente')

  function sincronizar() {
    setAvances(avances.map((a) => ({ ...a, estado: 'Enviado' })))
    setEnviados(true)
  }

  return (
    <Tarjeta titulo="Sincronización">
      <ul className="lista">
        {avances.map((a) => (
          <li key={a.fecha} className="item">
            <div className="item-cabecera">
              <span>Avance {a.fecha}</span>
              <span className={a.estado === 'Enviado' ? 'chip chip-Aprobado' : 'chip chip-Pendiente'}>{a.estado}</span>
            </div>
          </li>
        ))}
      </ul>
      <div className="menu">
        <button className="boton" onClick={sincronizar} disabled={!hayPendientes}>
          Sincronizar ahora
        </button>
        {hayPendientes && <Aviso tipo="info">Sin señal: se enviará al recuperar conexión.</Aviso>}
        {enviados && <Aviso tipo="ok">Avances enviados. El analista valida cada avance recibido.</Aviso>}
        <Link className="boton boton-secundario" to="/avance">
          Registrar otro avance
        </Link>
      </div>
    </Tarjeta>
  )
}
