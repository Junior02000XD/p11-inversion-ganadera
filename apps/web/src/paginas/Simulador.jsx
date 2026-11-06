import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { formatoBs } from '../api'
import { Aviso, Campo, Datos, Tarjeta } from '../componentes'
import { buscarProyecto } from '../datosEjemplo'

/** RF-12: tres escenarios (pesimista, esperado, optimista) para el monto que se quiere invertir (mockup 05b). */
export default function Simulador() {
  const proyecto = buscarProyecto(useParams().id)
  const [monto, setMonto] = useState('1000')
  const [escenarios, setEscenarios] = useState(null)

  function calcular(e) {
    e.preventDefault()
    const filas = Object.entries(proyecto.escenarios).map(([nombre, factor]) => [nombre, formatoBs(Number(monto) * factor)])
    setEscenarios(filas)
  }

  return (
    <Tarjeta titulo="Simulador" subtitulo={proyecto.nombre}>
      <form onSubmit={calcular} className="formulario">
        <Campo etiqueta="Monto a simular (Bs)">
          <input type="number" min={proyecto.minimoBs} value={monto} onChange={(e) => setMonto(e.target.value)} />
        </Campo>
        <button className="boton">Calcular escenarios</button>
      </form>

      {escenarios && (
        <>
          <Datos filas={escenarios} />
          <Aviso tipo="info">Estimación ilustrativa, no garantizada.</Aviso>
        </>
      )}
      <div className="menu">
        <Link className="boton" to={`/catalogo/${proyecto.id}/invertir`}>
          Invertir en este proyecto
        </Link>
      </div>
    </Tarjeta>
  )
}
