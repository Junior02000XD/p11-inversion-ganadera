import { useState } from 'react'
import { useParams } from 'react-router'
import { formatoBs } from '../../api'
import { Aviso, Datos, Tarjeta } from '../../componentes'
import { buscarProyecto, liquidacion, resultadoLiquidacion } from '../../datosEjemplo'

/** RF-23 y RF-24: el sistema calcula el resultado y el analista lo aprueba (mockup 12b). */
export default function Liquidacion() {
  const proyecto = buscarProyecto(useParams().id)
  const [aprobada, setAprobada] = useState(false)

  return (
    <Tarjeta titulo={`Liquidación: ${proyecto.nombre}`}>
      <Datos
        filas={[
          ['Venta', formatoBs(liquidacion.ventaBs)],
          ['− Costos', formatoBs(liquidacion.costosBs)],
          ['− Pérdidas', formatoBs(liquidacion.perdidasBs)],
          ['− Comisión', formatoBs(liquidacion.comisionBs)],
        ]}
        total={['Resultado a repartir', formatoBs(resultadoLiquidacion(liquidacion))]}
      />
      <div className="formulario">
        <Aviso tipo="info">Cifras calculadas con los datos que cargó el productor.</Aviso>
        {aprobada ? (
          <Aviso tipo="ok">Liquidación aprobada. Se avisa a cada inversionista para que dé su conformidad.</Aviso>
        ) : (
          <>
            <button className="boton" onClick={() => setAprobada(true)}>
              Aprobar liquidación
            </button>
            <button className="boton boton-secundario">Recalcular</button>
          </>
        )}
      </div>
    </Tarjeta>
  )
}
