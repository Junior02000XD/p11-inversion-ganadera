import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { api, ErrorApi, formatoHora, mensajeDe } from '../../api'
import { Aviso, Campo, Tarjeta } from '../../componentes'
import { useSesion } from '../../sesion'

/** RF-02: inicio de sesión con aviso de intentos restantes y bloqueo al 5.º fallo (mockup 02a). */
export default function Login() {
  const { usuario, entrar } = useSesion()
  const navegar = useNavigate()
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState('')
  const [bloqueado, setBloqueado] = useState(false)
  const [requiereOtp, setRequiereOtp] = useState(false)
  const [enviando, setEnviando] = useState(false)

  if (usuario) return <Navigate to="/inicio" replace />

  async function enviar(e) {
    e.preventDefault()
    setEnviando(true)
    setError('')
    setBloqueado(false)
    setRequiereOtp(false)
    try {
      // Respuesta: { token, usuario }
      const sesion = await api('/auth/login', { cuerpo: { correo, contrasena } })
      entrar(sesion.token, sesion.usuario)
      navegar('/inicio')
    } catch (err) {
      if (!(err instanceof ErrorApi)) return setError(mensajeDe(err))
      const restantes = err.datos.intentosRestantes
      if (err.status === 423) {
        setBloqueado(true)
        const hasta = err.datos.bloqueadoHasta
        setError(
          hasta
            ? `Tu cuenta quedó bloqueada por intentos fallidos. Vuelve a intentarlo a las ${formatoHora(hasta)}. Te avisamos por correo.`
            : err.message,
        )
      } else if (err.status === 403 && err.datos.requiereOtp) {
        setRequiereOtp(true)
        setError(err.message)
      } else if (err.status === 401 && typeof restantes === 'number') {
        setError(`Credenciales incorrectas. ${restantes === 1 ? 'Queda 1 intento' : `Quedan ${restantes} intentos`}.`)
      } else setError(err.message)
    } finally {
      setEnviando(false)
    }
  }

  return (
    <Tarjeta titulo="Iniciar sesión" subtitulo="Accede a tu cuenta de inversionista, productor o del equipo.">
      <form onSubmit={enviar} className="formulario">
        <Campo etiqueta="Correo">
          <input type="email" required autoComplete="email" placeholder="nombre@correo.com" value={correo} onChange={(e) => setCorreo(e.target.value)} />
        </Campo>
        <Campo etiqueta="Contraseña">
          <input type="password" required autoComplete="current-password" value={contrasena} onChange={(e) => setContrasena(e.target.value)} />
        </Campo>
        <button className="boton" disabled={enviando}>
          {enviando ? 'Ingresando…' : 'Ingresar'}
        </button>
        <Aviso>{error}</Aviso>
        {bloqueado && <p className="nota">Si no fuiste tú, cambia tu contraseña cuando se levante el bloqueo.</p>}
        {requiereOtp && (
          <Link className="boton boton-secundario" to={`/verificar-cuenta?correo=${encodeURIComponent(correo)}`}>
            Ingresar el código
          </Link>
        )}
      </form>
      <p className="pie">
        ¿No tienes cuenta? <Link to="/registro">Crear cuenta</Link>
      </p>
    </Tarjeta>
  )
}
