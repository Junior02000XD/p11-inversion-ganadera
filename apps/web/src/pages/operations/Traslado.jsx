import { useState } from 'react'
import { Archivo, Aviso, Campo, Tarjeta } from '../../componentes'

/** RNF-11: cada traslado lleva su Guía de Movimiento Animal (GMA) de SENASAG (mockup 09b). */
export default function Traslado() {
  const [guia, setGuia] = useState(null)
  const [aviso, setAviso] = useState(null)

  function confirmar(e) {
    e.preventDefault()
    if (!guia) return setAviso({ tipo: 'error', texto: 'Adjunta la guía GMA.' })
    setAviso({ tipo: 'ok', texto: 'Traslado registrado. La guía queda en el expediente del proyecto.' })
  }

  return (
    <Tarjeta titulo="Registrar traslado" subtitulo="Compra de 120 cabezas a Hacienda San Antonio">
      <form onSubmit={confirmar} className="formulario">
        <Campo etiqueta="Nº de guía GMA">
          <input placeholder="GMA-000123" />
        </Campo>
        <div className="fila">
          <Campo etiqueta="Origen">
            <input placeholder="San Antonio" />
          </Campo>
          <Campo etiqueta="Destino">
            <input placeholder="Predio del productor" />
          </Campo>
        </div>
        <Campo etiqueta="Fecha de llegada">
          <input type="date" />
        </Campo>
        <Campo etiqueta="Guía escaneada">
          <Archivo texto="Adjuntar guía" alElegir={setGuia} />
        </Campo>
        <button className="boton">Confirmar traslado</button>
        {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
      </form>
    </Tarjeta>
  )
}
