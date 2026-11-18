import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { proyectos } from '../datosEjemplo'

/** RNF-11: con la ronda financiada se registra la compra del ganado (mockup 09a). */
export default function Compra() {
  const navegar = useNavigate()
  const [proveedor, setProveedor] = useState('')
  const [error, setError] = useState('')

  function guardar(e) {
    e.preventDefault()
    if (!proveedor.trim()) return setError('Faltan datos del proveedor.')
    navegar('/traslado')
  }

  return (
    <Tarjeta titulo="Registrar compra">
      <form onSubmit={guardar} className="formulario">
        <Campo etiqueta="Proyecto">
          <select>
            {proyectos
              .filter((p) => p.financiado)
              .map((p) => (
                <option key={p.id}>{p.nombre}</option>
              ))}
          </select>
        </Campo>
        <Campo etiqueta="Cabezas compradas">
          <input type="number" min="0" placeholder="120" />
        </Campo>
        <Campo etiqueta="Proveedor">
          <input placeholder="Hacienda San Antonio" value={proveedor} onChange={(e) => setProveedor(e.target.value)} />
        </Campo>
        <Campo etiqueta="Costo total (Bs)">
          <input type="number" min="0" placeholder="62.000" />
        </Campo>
        <button className="boton">Guardar compra</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
