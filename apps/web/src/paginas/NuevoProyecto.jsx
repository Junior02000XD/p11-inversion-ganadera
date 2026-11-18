import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { api, ErrorApi, mensajeDe } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { ADMINISTRADOR, PRODUCTOR } from '../roles'
import { useSesion } from '../sesion'

// Cada campo numérico se describe con { clave, etiqueta, paso?, placeholder? }.
// `clave` es el nombre que espera la API; `paso` es el step del input (1 = solo enteros).

const comunes = [
  {
    clave: 'montoRequeridoBs',
    etiqueta: 'Monto requerido (Bs)',
    placeholder: '100000',
  },
  {
    clave: 'montoMinimoBs',
    etiqueta: 'Inversión mínima (Bs)',
    placeholder: '100',
  },
  {
    clave: 'plazoMeses',
    etiqueta: 'Plazo (meses)',
    paso: '1',
    placeholder: '10',
  },
]

// RF-07: al elegir la línea solo se muestran sus campos.
const porLinea = {
  Engorde: [
    {
      clave: 'cantidadAnimales',
      etiqueta: 'Cabezas de ganado',
      paso: '1',
      placeholder: '120',
    },
    {
      clave: 'pesoInicialKg',
      etiqueta: 'Peso inicial promedio (kg)',
      placeholder: '180',
    },
    { clave: 'pesoMetaKg', etiqueta: 'Peso meta (kg)', placeholder: '420' },
    {
      clave: 'costosEstimadosBs',
      etiqueta: 'Costos estimados (Bs)',
      placeholder: '30000',
    },
  ],
  Lecheria: [
    {
      clave: 'vacasEnOrdeno',
      etiqueta: 'Vacas en ordeño',
      paso: '1',
      placeholder: '30',
    },
    { clave: 'litrosPorDia', etiqueta: 'Litros por día', placeholder: '450' },
    {
      clave: 'precioLitroBs',
      etiqueta: 'Precio del litro (Bs)',
      placeholder: '4.20',
    },
  ],
}

const pascal = (s) => s.charAt(0).toUpperCase() + s.slice(1)

/**
 * RF-07: ficha del proyecto guardada como borrador (mockup 03a). Con `:id` en la ruta corrige un borrador
 * (PUT; la línea no cambia). El administrador postula en nombre de un productor que elige de la lista.
 */
export default function NuevoProyecto() {
  const navegar = useNavigate()
  const { id } = useParams()
  const editando = Boolean(id)
  const esAdmin = useSesion().usuario?.rol === ADMINISTRADOR
  const [productores, setProductores] = useState([])
  const [productorId, setProductorId] = useState('')
  const [linea, setLinea] = useState('Engorde')
  const [nombre, setNombre] = useState('')
  const [ubicacion, setUbicacion] = useState('')
  const [valores, setValores] = useState({})
  const [campos, setCampos] = useState({})
  const [error, setError] = useState('')

  // Al editar se cargan los datos actuales de la ficha.
  useEffect(() => {
    if (!editando) return
    api(`/proyectos/${id}`)
      .then((p) => {
        setLinea(p.linea)
        setNombre(p.nombre)
        setUbicacion(p.ubicacion)
        setValores(Object.fromEntries([...comunes, ...porLinea[p.linea]].map((c) => [c.clave, p[c.clave] ?? ''])))
      })
      .catch((err) => setError(mensajeDe(err)))
  }, [editando, id])

  useEffect(() => {
    if (!esAdmin || editando) return
    api('/admin/usuarios')
      .then((us) => setProductores(us.filter((u) => u.rol === PRODUCTOR)))
      .catch((err) => setError(mensajeDe(err)))
  }, [esAdmin, editando])

  const limpiar = (campo) => {
    if (!campos[campo]) return
    const { [campo]: _, ...resto } = campos
    setCampos(resto)
    if (Object.keys(resto).length === 0) setError('')
  }

  async function guardar(e) {
    e.preventDefault()
    setError('')
    setCampos({})
    const numeros = Object.fromEntries([...comunes, ...porLinea[linea]].map((c) => [c.clave, valores[c.clave] ? Number(valores[c.clave]) : 0]))
    try {
      const cuerpo = { linea, nombre, ubicacion, ...numeros }
      if (editando) {
        await api(`/proyectos/${id}`, { metodo: 'PUT', cuerpo })
        navegar('/proyectos')
        return
      }
      if (esAdmin) cuerpo.productorId = productorId || null
      await api('/proyectos', { cuerpo })
      navegar('/proyectos', { state: { creado: nombre } })
    } catch (err) {
      if (err instanceof ErrorApi) {
        setCampos(err.campos)
        setError(Object.keys(err.campos).length ? 'Completa los campos obligatorios.' : err.message)
      } else setError(mensajeDe(err))
    }
  }

  const input = (c) => (
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
    <Tarjeta
      titulo={editando ? 'Editar proyecto' : 'Nuevo proyecto'}
      subtitulo={
        editando
          ? 'Solo un borrador se puede corregir. La línea productiva no cambia.'
          : 'Se guarda como borrador. Puedes enviarlo a evaluación cuando tus documentos estén aprobados.'
      }
    >
      <form onSubmit={guardar} className="formulario" noValidate>
        {esAdmin && !editando && (
          <Campo etiqueta="Productor" error={campos.ProductorId}>
            <select value={productorId} onChange={(e) => setProductorId(e.target.value)}>
              <option value="">Elige un productor…</option>
              {productores.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.nombre} · {u.correo}
                </option>
              ))}
            </select>
          </Campo>
        )}
        <Campo etiqueta="Línea">
          <div className="segmentado" role="radiogroup">
            {['Engorde', 'Lecheria'].map((l) => (
              <button
                type="button"
                key={l}
                role="radio"
                disabled={editando}
                aria-checked={linea === l}
                className={linea === l ? 'activo' : ''}
                onClick={() => {
                  setLinea(l)
                  setCampos({})
                  setError('')
                }}
              >
                {l === 'Lecheria' ? 'Lechería' : 'Engorde'}
              </button>
            ))}
          </div>
        </Campo>
        <Campo etiqueta="Nombre del proyecto" error={campos.Nombre}>
          <input
            placeholder={linea === 'Engorde' ? 'Engorde Chiquitania 2026' : 'Tambo San Juan'}
            value={nombre}
            onChange={(e) => {
              setNombre(e.target.value)
              limpiar('Nombre')
            }}
          />
        </Campo>
        <Campo etiqueta="Ubicación del predio" error={campos.Ubicacion}>
          <input
            placeholder="Municipio, provincia"
            value={ubicacion}
            onChange={(e) => {
              setUbicacion(e.target.value)
              limpiar('Ubicacion')
            }}
          />
        </Campo>
        <fieldset className="grupo">
          <legend>{linea === 'Engorde' ? 'Datos de engorde' : 'Datos de lechería'}</legend>
          <div className="rejilla">{porLinea[linea].map(input)}</div>
        </fieldset>
        <fieldset className="grupo">
          <legend>Financiamiento</legend>
          <div className="rejilla">{comunes.map(input)}</div>
        </fieldset>
        <button className="boton">{editando ? 'Guardar cambios' : 'Guardar borrador'}</button>
        <Aviso>{error}</Aviso>
      </form>
    </Tarjeta>
  )
}
