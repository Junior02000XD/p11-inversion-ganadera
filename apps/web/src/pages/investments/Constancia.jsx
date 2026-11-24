import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router'
import { api, formatoBs, formatoFecha, formatoPct, mensajeDe, textoLinea } from '../../api'
import { Aviso, Datos, Tarjeta } from '../../componentes'

// GET /portafolio/:id/constancia → { codigo, inversionista, ci, inversion: {...}, ubicacionProyecto, plazoMeses }

/** RF-16: constancia de participación de una inversión pagada. Se puede imprimir o guardar como PDF. */
export default function Constancia() {
  const { id } = useParams()
  const recien = useLocation().state?.recien
  const [constancia, setConstancia] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api(`/portafolio/${id}/constancia`)
      .then(setConstancia)
      .catch((err) => setError(mensajeDe(err)))
  }, [id])

  if (!constancia) return <Tarjeta titulo="Constancia de participación">{error ? <Aviso>{error}</Aviso> : <p className="tenue">Cargando…</p>}</Tarjeta>

  const i = constancia.inversion
  return (
    <Tarjeta titulo="Constancia de participación" subtitulo={`N.º ${constancia.codigo}`}>
      {recien && <Aviso tipo="ok">Pago confirmado. Tu participación ya está en el portafolio.</Aviso>}
      <Datos
        filas={[
          ['Inversionista', `${constancia.inversionista} (CI ${constancia.ci})`],
          ['Proyecto', `${i.proyecto} · ${textoLinea(i.linea)}`],
          ['Ubicación', constancia.ubicacionProyecto],
          ['Plazo', `${constancia.plazoMeses} meses`],
          ['Aporte', formatoBs(i.montoBs)],
          ['Participación', formatoPct(i.participacionPct)],
          ...(i.participacionKg ? [['Equivalente en ganado', `≈ ${i.participacionKg.toLocaleString('es-BO')} kg`]] : []),
          ['Pago confirmado', formatoFecha(i.pagadaEn)],
          ['Referencia de pago', i.referenciaPago],
          ['Contrato aceptado', `versión ${i.contratoVersion}, ${formatoFecha(i.contratoAceptadoEn)}`],
        ]}
      />
      <p className="nota">La rentabilidad es una estimación y no está garantizada.</p>
      <div className="menu no-imprimir">
        <button className="boton" onClick={() => window.print()}>
          Imprimir o guardar PDF
        </button>
        <Link className="boton boton-secundario" to="/portafolio">
          Ir al portafolio
        </Link>
      </div>
    </Tarjeta>
  )
}
