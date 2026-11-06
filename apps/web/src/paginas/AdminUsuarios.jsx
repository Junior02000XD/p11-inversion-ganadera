import { useEffect, useMemo, useState } from 'react'
import { api, mensajeDe, textoEstado } from '../api'
import { Aviso, Campo, Tarjeta } from '../componentes'
import { useSesion } from '../sesion'

// Cada usuario llega como { id, nombre, correo, ci, rol, estadoVerificacion, correoConfirmado, bloqueadoHasta }.

const roles = ['Inversionista', 'Productor', 'Analista', 'Administrador']

/** RF-06: usuarios y roles (mockup 02c). */
export default function AdminUsuarios() {
  const { usuario: yo } = useSesion()
  const [usuarios, setUsuarios] = useState([])
  const [buscar, setBuscar] = useState('')
  const [elegido, setElegido] = useState(null)
  const [rol, setRol] = useState('Inversionista')
  const [aviso, setAviso] = useState(null)

  const cargar = () => api('/admin/usuarios').then(setUsuarios)
  useEffect(() => {
    cargar()
  }, [])

  const filtrados = useMemo(() => {
    const q = buscar.trim().toLowerCase()
    return q ? usuarios.filter((u) => `${u.nombre} ${u.correo} ${u.ci}`.toLowerCase().includes(q)) : usuarios
  }, [usuarios, buscar])

  async function ejecutar(accion) {
    setAviso(null)
    try {
      setAviso({ tipo: 'ok', texto: (await accion()).mensaje })
      await cargar()
      setElegido(null)
    } catch (err) {
      setAviso({ tipo: 'error', texto: mensajeDe(err) })
    }
  }

  return (
    <Tarjeta titulo="Usuarios y roles" ancha>
      <Campo etiqueta="Buscar usuario">
        <input placeholder="Nombre, correo o CI" value={buscar} onChange={(e) => setBuscar(e.target.value)} />
      </Campo>
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
      <div className="tabla-scroll">
        <table className="tabla">
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Rol</th>
              <th>Identidad</th>
              <th>Cuenta</th>
            </tr>
          </thead>
          <tbody>
            {filtrados.map((u) => (
              <tr
                key={u.id}
                className={elegido?.id === u.id ? 'fila-activa' : 'fila-clic'}
                onClick={() => {
                  setElegido(u)
                  setRol(u.rol)
                }}
              >
                <td>
                  <strong>{u.nombre}</strong>
                  <div className="tenue">{u.correo}</div>
                </td>
                <td>{u.rol}</td>
                <td>{textoEstado[u.estadoVerificacion]}</td>
                <td>
                  {u.bloqueadoHasta ? (
                    <span className="chip chip-Rechazado">Bloqueada</span>
                  ) : u.correoConfirmado ? (
                    <span className="chip chip-Aprobado">Activa</span>
                  ) : (
                    <span className="chip">Sin confirmar</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {elegido && (
        <div className="panel">
          <h2>{elegido.nombre}</h2>
          <Campo etiqueta="Rol asignado">
            <select value={rol} onChange={(e) => setRol(e.target.value)} disabled={elegido.id === yo?.id}>
              {roles.map((r) => (
                <option key={r}>{r}</option>
              ))}
            </select>
          </Campo>
          {elegido.id === yo?.id && <p className="nota">No puedes cambiar tu propio rol.</p>}
          <div className="acciones">
            <button
              className="boton"
              disabled={elegido.id === yo?.id || rol === elegido.rol}
              onClick={() => ejecutar(() => api(`/admin/usuarios/${elegido.id}/rol`, { metodo: 'PUT', cuerpo: { rol } }))}
            >
              Guardar rol
            </button>
            {elegido.bloqueadoHasta && (
              <button className="boton boton-secundario" onClick={() => ejecutar(() => api(`/admin/usuarios/${elegido.id}/desbloquear`, { metodo: 'POST' }))}>
                Desbloquear cuenta
              </button>
            )}
          </div>
        </div>
      )}
    </Tarjeta>
  )
}
