import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api, mensajeDe } from '../../api'
import { Aviso, Campo, Tarjeta } from '../../componentes'

/** RNF-01: confirmación de la cuenta con el código de 6 dígitos enviado por correo (mockup 01b). */
export default function VerificarCuenta() {
  const [params] = useSearchParams()
  const [correo, setCorreo] = useState(params.get('correo') ?? '')
  const [codigo, setCodigo] = useState('')
  const [error, setError] = useState('')
  const [ok, setOk] = useState('')
  const [confirmada, setConfirmada] = useState(false)

  async function validar(e) {
    e.preventDefault()
    setError('')
    setOk('')
    try {
      const r = await api('/auth/otp/verificar', { cuerpo: { correo, codigo } })
      setOk(r.mensaje)
      setConfirmada(true)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  async function reenviar() {
    setError('')
    try {
      const r = await api('/auth/otp/reenviar', { cuerpo: { correo } })
      setOk(r.mensaje)
    } catch (err) {
      setError(mensajeDe(err))
    }
  }

  return (
    <Tarjeta titulo="Verifica tu cuenta" subtitulo="Te enviamos un código de 6 dígitos a tu correo. Vence en 10 minutos.">
      {confirmada ? (
        <>
          <Aviso tipo="ok">{ok}</Aviso>
          <Link className="boton" to="/ingresar">
            Iniciar sesión
          </Link>
        </>
      ) : (
        <form onSubmit={validar} className="formulario">
          {!params.get('correo') && (
            <Campo etiqueta="Correo">
              <input type="email" required value={correo} onChange={(e) => setCorreo(e.target.value)} />
            </Campo>
          )}
          <Campo etiqueta="Código OTP">
            <input
              className="otp"
              required
              inputMode="numeric"
              autoComplete="one-time-code"
              pattern="\d{6}"
              maxLength={6}
              placeholder="______"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
            />
          </Campo>
          <button className="boton">Validar código</button>
          <button type="button" className="boton boton-secundario" onClick={reenviar} disabled={!correo}>
            Reenviar código
          </button>
          <Aviso>{error}</Aviso>
          <Aviso tipo="info">{ok}</Aviso>
        </form>
      )}
    </Tarjeta>
  )
}
