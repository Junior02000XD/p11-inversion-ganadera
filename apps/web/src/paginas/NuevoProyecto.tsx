import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api, ErrorApi, mensajeDe } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'

type Linea = 'Engorde' | 'Lecheria'

interface DefCampo {
  clave: string
  etiqueta: string
  paso?: string
  placeholder?: string
}

const comunes: DefCampo[] = [
  { clave: 'montoRequeridoBs', etiqueta: 'Monto requerido (Bs)', placeholder: '100000' },
  { clave: 'montoMinimoBs', etiqueta: 'Inversión mínima (Bs)', placeholder: '100' },
  { clave: 'plazoMeses', etiqueta: 'Plazo (meses)', paso: '1', placeholder: '10' },
]

// RF-07: al elegir la línea solo se muestran sus campos.
const porLinea: Record<Linea, DefCampo[]> = {
  Engorde: [
    { clave: 'cantidadAnimales', etiqueta: 'Cabezas de ganado', paso: '1', placeholder: '120' },
    { clave: 'pesoInicialKg', etiqueta: 'Peso inicial promedio (kg)', placeholder: '180' },
    { clave: 'pesoMetaKg', etiqueta: 'Peso meta (kg)', placeholder: '420' },
    { clave: 'costosEstimadosBs', etiqueta: 'Costos estimados (Bs)', placeholder: '30000' },
  ],
  Lecheria: [
    { clave: 'vacasEnOrdeno', etiqueta: 'Vacas en ordeño', paso: '1', placeholder: '30' },
    { clave: 'litrosPorDia', etiqueta: 'Litros por día', placeholder: '450' },
    { clave: 'precioLitroBs', etiqueta: 'Precio del litro (Bs)', placeholder: '4.20' },
  ],
}

const pascal = (s: string) => s.charAt(0).toUpperCase() + s.slice(1)

/** RF-07: ficha del proyecto guardada como borrador (mockup 03a). */
export default function NuevoProyecto() {
  const navegar = useNavigate()
  const [linea, setLinea] = useState<Linea>('Engorde')
  const [nombre, setNombre] = useState('')
  const [ubicacion, setUbicacion] = useState('')
  const [valores, setValores] = useState<Record<string, string>>({})
  const [campos, setCampos] = useState<Record<string, string[]>>({})
  const [error, setError] = useState('')

  const limpiar = (campo: string) => {
    if (!campos[campo]) return
    const { [campo]: _, ...resto } = campos
    setCampos(resto)
    if (Object.keys(resto).length === 0) setError('')
  }

  async function guardar(e: FormEvent) {
    e.preventDefault()
    setError('')
    setCampos({})
    const numeros = Object.fromEntries(
      [...comunes, ...porLinea[linea]].map((c) => [c.clave, valores[c.clave] ? Number(valores[c.clave]) : 0]),
    )
    try {
      await api('/proyectos', { cuerpo: { linea, nombre, ubicacion, ...numeros } })
      navegar('/proyectos', { state: { creado: nombre } })
    } catch (err) {
      if (err instanceof ErrorApi) {
        setCampos(err.campos)
        setError(Object.keys(err.campos).length ? 'Completa los campos obligatorios.' : err.message)
      } else setError(mensajeDe(err))
    }
  }

  const input = (c: DefCampo) => (
    <Campo key={c.clave} etiqueta={c.etiqueta} error={campos[pascal(c.clave)]}>
      <input
        type="number"
        inputMode="decimal"
        min="0"
        step={c.paso ?? 'any'}
        placeholder={c.placeholder && `Ej. ${c.placeholder}`}
        value={valores[c.clave] ?? ''}
        onChange={(e) => {
          setValores({ ...valores, [c.clave]: e.target.value })
          limpiar(pascal(c.clave))
        }}
      />
    </Campo>
  )

  return (
    <Tarjeta titulo="Nuevo proyecto" subtitulo="Se guarda como borrador. Puedes enviarlo a evaluación cuando tus documentos estén aprobados.">
      <form onSubmit={guardar} className="formulario" noValidate>
        <Campo etiqueta="Línea">
          <div className="segmentado" role="radiogroup">
            {(['Engorde', 'Lecheria'] as Linea[]).map((l) => (
              <button type="button" key={l} role="radio" aria-checked={linea === l} className={linea === l ? 'activo' : ''} onClick={() => {
                  setLinea(l)
                  setCampos({})
                  setError('')
                }}>
                {l === 'Lecheria' ? 'Lechería' : 'Engorde'}
              </button>
            ))}
          </div>
        </Campo>
        <Campo etiqueta="Nombre del proyecto" error={campos.Nombre}>
          <input placeholder={linea === 'Engorde' ? 'Engorde Chiquitania 2026' : 'Tambo San Juan'} value={nombre} onChange={(e) => {
              setNombre(e.target.value)
              limpiar('Nombre')
            }} />
        </Campo>
        <Campo etiqueta="Ubicación del predio" error={campos.Ubicacion}>
          <input placeholder="Municipio, provincia" value={ubicacion} onChange={(e) => {
              setUbicacion(e.target.value)
              limpiar('Ubicacion')
            }} />
        </Campo>
        <fieldset className="grupo">
          <legend>{linea === 'Engorde' ? 'Datos de engorde' : 'Datos de lechería'}</legend>
          <div className="rejilla">{porLinea[linea].map(input)}</div>
        </fieldset>
        <fieldset className="grupo">
          <legend>Financiamiento</legend>
          <div className="rejilla">{comunes.map(input)}</div>
        </fieldset>
        <button className="boton">Guardar borrador</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
