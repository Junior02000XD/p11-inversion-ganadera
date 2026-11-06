import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { Marco, RequiereSesion } from './componentes'
import { ProveedorSesion } from './sesion'
import Login from './paginas/Login'
import Registro from './paginas/Registro'
import VerificarCuenta from './paginas/VerificarCuenta'
import Inicio from './paginas/Inicio'
import Identidad from './paginas/Identidad'
import NuevoProyecto from './paginas/NuevoProyecto'
import MisProyectos from './paginas/MisProyectos'
import Documentos from './paginas/Documentos'
import PanelAnalista from './paginas/PanelAnalista'
import AdminUsuarios from './paginas/AdminUsuarios'
import './estilos.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ProveedorSesion>
      <BrowserRouter>
        <Routes>
          <Route element={<Marco />}>
            <Route path="/ingresar" element={<Login />} />
            <Route path="/registro" element={<Registro />} />
            <Route path="/verificar-cuenta" element={<VerificarCuenta />} />
            <Route element={<RequiereSesion />}>
              <Route path="/inicio" element={<Inicio />} />
            </Route>
            <Route element={<RequiereSesion roles={['Inversionista', 'Productor']} />}>
              <Route path="/identidad" element={<Identidad />} />
            </Route>
            <Route element={<RequiereSesion roles={['Productor']} />}>
              <Route path="/proyectos" element={<MisProyectos />} />
              <Route path="/proyectos/nuevo" element={<NuevoProyecto />} />
              <Route path="/documentos" element={<Documentos />} />
            </Route>
            <Route element={<RequiereSesion roles={['Analista']} />}>
              <Route path="/analista" element={<PanelAnalista />} />
            </Route>
            <Route element={<RequiereSesion roles={['Administrador']} />}>
              <Route path="/admin/usuarios" element={<AdminUsuarios />} />
            </Route>
            <Route path="*" element={<Navigate to="/inicio" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ProveedorSesion>
  </StrictMode>,
)
