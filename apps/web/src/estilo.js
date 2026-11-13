// Estilo visual elegido por el usuario: 'pampa' (por defecto) o 'cuero'.
// Se guarda en localStorage y se aplica como data-estilo en <html>; estilos.css hace el resto.

export const ESTILOS = [
  { id: 'pampa', nombre: 'Pampa', color: '#5c6e4a' },
  { id: 'cuero', nombre: 'Cuero', color: '#6e4423' },
]
const CLAVE = 'p11.estilo'

export function estiloGuardado() {
  try {
    const e = localStorage.getItem(CLAVE)
    return ESTILOS.some((x) => x.id === e) ? e : 'pampa'
  } catch {
    return 'pampa' // almacenamiento bloqueado: se usa el de por defecto
  }
}

export function aplicarEstilo(id) {
  const raiz = document.documentElement
  if (id === 'pampa') delete raiz.dataset.estilo
  else raiz.dataset.estilo = id
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', ESTILOS.find((x) => x.id === id)?.color ?? '#5c6e4a')
  try {
    localStorage.setItem(CLAVE, id)
  } catch {
    // sin almacenamiento: dura hasta recargar
  }
}
