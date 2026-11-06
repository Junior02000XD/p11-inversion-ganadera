import { useState } from 'react'
import { Archivo, Aviso, Campo, Tarjeta } from '../componentes'

/** RF-23: al vender el ganado, el productor carga la venta y los costos para liquidar (mockup 12a). */
export default function CierreProyecto() {
  const [factura, setFactura] = useState(null)
  const [aviso, setAviso] = useState(null)

  function enviar(e) {
    e.preventDefault()
    if (!factura) return setAviso({ tipo: 'error', texto: 'Falta la factura de venta.' })
    setAviso({ tipo: 'ok', texto: 'Enviado. El analista calcula y revisa la liquidación.' })
  }

  return (
    <Tarjeta titulo="Cierre del proyecto" subtitulo="Engorde Chiquitania 2026">
      <form onSubmit={enviar} className="formulario">
        <Campo etiqueta="Venta total (Bs)">
          <input type="number" min="0" placeholder="130.000" />
        </Campo>
        <Campo etiqueta="Costos (Bs)">
          <input type="number" min="0" placeholder="62.000" />
        </Campo>
        <Campo etiqueta="Factura de venta">
          <Archivo texto="Elegir archivo (PDF, JPG o PNG)" alElegir={setFactura} />
        </Campo>
        <button className="boton">Enviar para liquidar</button>
        {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
      </form>
    </Tarjeta>
  )
}
