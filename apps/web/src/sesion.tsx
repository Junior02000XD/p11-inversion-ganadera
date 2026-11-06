import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, guardarToken, hayToken, type UsuarioActual } from './api'

interface Sesion {
  usuario: UsuarioActual | null
  cargando: boolean
  entrar: (token: string, usuario: UsuarioActual) => void
  salir: () => void
  refrescar: () => Promise<void>
}

const Contexto = createContext<Sesion | null>(null)

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioActual | null>(null)
  const [cargando, setCargando] = useState(hayToken())

  const refrescar = useCallback(async () => {
    try {
      setUsuario(await api<UsuarioActual>('/auth/yo'))
    } catch {
      guardarToken(null)
      setUsuario(null)
    } finally {
      setCargando(false)
    }
  }, [])

  useEffect(() => {
    if (hayToken()) void refrescar()
  }, [refrescar])

  const entrar = (token: string, u: UsuarioActual) => {
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
