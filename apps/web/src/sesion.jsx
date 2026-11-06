import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { api, guardarToken, hayToken } from './api'

// La sesión se comparte con toda la app: { usuario, cargando, entrar, salir, refrescar }.
const Contexto = createContext(null)

export function ProveedorSesion({ children }) {
  const [usuario, setUsuario] = useState(null)
  const [cargando, setCargando] = useState(hayToken())

  const refrescar = useCallback(async () => {
    try {
      setUsuario(await api('/auth/yo'))
    } catch {
      guardarToken(null)
      setUsuario(null)
    } finally {
      setCargando(false)
    }
  }, [])

  useEffect(() => {
    if (hayToken()) refrescar()
  }, [refrescar])

  const entrar = (token, u) => {
    guardarToken(token)
    setUsuario(u)
  }
  const salir = () => {
    guardarToken(null)
    setUsuario(null)
  }

  return <Contexto.Provider value={{ usuario, cargando, entrar, salir, refrescar }}>{children}</Contexto.Provider>
}

export function useSesion() {
  const s = useContext(Contexto)
  if (!s) throw new Error('useSesion fuera de ProveedorSesion')
  return s
}
