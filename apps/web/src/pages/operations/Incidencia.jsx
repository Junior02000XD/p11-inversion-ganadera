import { useState } from 'react'
import { useParams } from 'react-router'
import { Archivo, Aviso, Datos, Tarjeta } from '../../componentes'

/** RF-20 y RF-21: seguimiento de una incidencia y lo que pide el analista (mockup 10b). */
export default function Incidencia() {
  const { id } = useParams()
  const [informe, setInforme] = useState(null)

  return (
    <Tarjeta titulo={`Incidencia #${id}`} subtitulo="Engorde Chiquitania 2026">
      <Datos
        filas={[
          ['Tipo', 'Mortalidad · 2 cabezas'],
          ['Estado', informe ? 'Informe recibido' : 'En evaluación'],
          ['Registrada', '12/10/2026'],
        ]}
      />
      <Aviso tipo="info">Analista: solicita informe veterinario.</Aviso>
      <div className="formulario">
        <Archivo texto="Adjuntar informe" alElegir={setInforme} />
        {informe && <Aviso tipo="ok">Informe enviado al analista.</Aviso>}
      </div>
      <p className="nota">Si aplica, se tramita el reclamo a la aseguradora.</p>
    </Tarjeta>
  )
}
