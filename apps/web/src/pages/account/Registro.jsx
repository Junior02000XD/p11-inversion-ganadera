import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { api, ErrorApi, mensajeDe } from '../../api'
import { Aviso, Campo, Tarjeta } from '../../componentes'

// Qué hace cada rol, para elegir con conocimiento (se muestra como ayuda bajo «Soy»).
const textoRol = {
  Inversionista: 'Aportas dinero a proyectos ganaderos publicados y recibes tu parte del resultado al venderse el ganado.',
  Productor: 'Tienes el ganado y el predio: postulas tu proyecto para conseguir financiamiento. Si te equivocas de rol, el administrador puede cambiarlo.',
}

/** RF-01: registro de inversionista o productor; al terminar se envía un OTP al correo (mockup 01a). */
export default function Registro() {
  const navegar = useNavigate()
  const [datos, setDatos] = useState({ nombre: '', correo: '', celular: '', ci: '', contrasena: '', rol: 'Inversionista' })
  const [acepta, setAcepta] = useState(false)
  const [error, setError] = useState('')
  const [campos, setCampos] = useState({})
  const [enviando, setEnviando] = useState(false)
  const cambiar = (campo) => (e) => setDatos({ ...datos, [campo]: e.target.value })

  async function enviar(e) {
    e.preventDefault()
    setEnviando(true)
    setError('')
    setCampos({})
    try {
      await api('/auth/registro', { cuerpo: datos })
      navegar(`/verificar-cuenta?correo=${encodeURIComponent(datos.correo)}`)
    } catch (err) {
      if (err instanceof ErrorApi) {
        setError(err.message)
        setCampos(err.campos)
      } else setError(mensajeDe(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <Tarjeta titulo="Crear cuenta" subtitulo="Inversionista o productor">
      <form onSubmit={enviar} className="formulario">
        <Campo etiqueta="Nombre completo" error={campos.Nombre}>
          <input required minLength={3} placeholder="Ej. María Rojas" value={datos.nombre} onChange={cambiar('nombre')} autoComplete="name" />
        </Campo>
        <Campo etiqueta="Correo electrónico" error={campos.Correo}>
          <input type="email" required placeholder="nombre@correo.com" value={datos.correo} onChange={cambiar('correo')} autoComplete="email" />
        </Campo>
        <div className="fila">
          <Campo etiqueta="Celular" error={campos.Celular}>
            <input required inputMode="tel" placeholder="+591 7xxxxxxx" value={datos.celular} onChange={(e) => setDatos({ ...datos, celular: e.target.value.replace(/[\s-]/g, '') })} autoComplete="tel" />
          </Campo>
          <Campo etiqueta="CI" ayuda="Solo números. Si tu carnet tiene complemento, va después de un guion: 1234567-1B." error={campos.Ci}>
            <input required placeholder="1234567" value={datos.ci} onChange={cambiar('ci')} />
          </Campo>
        </div>
        <Campo etiqueta="Contraseña" error={campos.Contrasena} ayuda="Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.">
          <input type="password" required minLength={8} value={datos.contrasena} onChange={cambiar('contrasena')} autoComplete="new-password" />
        </Campo>
        <Campo etiqueta="Soy" ayuda={textoRol[datos.rol]} error={campos.Rol}>
          <select value={datos.rol} onChange={cambiar('rol')}>
            <option value="Inversionista">Inversionista</option>
            <option value="Productor">Productor</option>
          </select>
        </Campo>
        <label className="casilla">
          <input type="checkbox" checked={acepta} onChange={(e) => setAcepta(e.target.checked)} required />
          Acepto los términos y el aviso de riesgos
        </label>
        <button className="boton" disabled={enviando || !acepta}>
          {enviando ? 'Creando…' : 'Crear cuenta'}
        </button>
        <Aviso>{error}</Aviso>
      </form>
      <p className="pie">
        ¿Ya tienes cuenta? <Link to="/ingresar">Iniciar sesión</Link>
      </p>
    </Tarjeta>
  )
}
