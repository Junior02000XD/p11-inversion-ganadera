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
// Sprints 2 y 3 (por ahora con datos de ejemplo, ver datosEjemplo.js)
import Catalogo from './paginas/Catalogo'
import Simulador from './paginas/Simulador'
import Invertir from './paginas/Invertir'
import Contrato from './paginas/Contrato'
import PagoQr from './paginas/PagoQr'
import Ronda from './paginas/Ronda'
import Portafolio from './paginas/Portafolio'
import Expediente from './paginas/Expediente'
import MiLiquidacion from './paginas/MiLiquidacion'
import Avance from './paginas/Avance'
import AvancesPendientes from './paginas/AvancesPendientes'
import NuevaIncidencia from './paginas/NuevaIncidencia'
import Incidencia from './paginas/Incidencia'
import CierreProyecto from './paginas/CierreProyecto'
import Evaluacion from './paginas/Evaluacion'
import EvaluarProyecto from './paginas/EvaluarProyecto'
import Compra from './paginas/Compra'
import Traslado from './paginas/Traslado'
import Liquidacion from './paginas/Liquidacion'
import './estilos.css'

createRoot(document.getElementById('root')).render(
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
            <Route element={<RequiereSesion roles={['Inversionista']} />}>
              <Route path="/catalogo" element={<Catalogo />} />
              <Route path="/catalogo/:id" element={<Simulador />} />
              <Route path="/catalogo/:id/invertir" element={<Invertir />} />
              <Route path="/catalogo/:id/contrato" element={<Contrato />} />
              <Route path="/catalogo/:id/pago" element={<PagoQr />} />
              <Route path="/catalogo/:id/ronda" element={<Ronda />} />
              <Route path="/portafolio" element={<Portafolio />} />
              <Route path="/portafolio/:id" element={<Expediente />} />
              <Route path="/portafolio/:id/liquidacion" element={<MiLiquidacion />} />
            </Route>
            <Route element={<RequiereSesion roles={['Productor']} />}>
              <Route path="/proyectos" element={<MisProyectos />} />
              <Route path="/proyectos/nuevo" element={<NuevoProyecto />} />
              <Route path="/documentos" element={<Documentos />} />
              <Route path="/avance" element={<Avance />} />
              <Route path="/avance/pendientes" element={<AvancesPendientes />} />
              <Route path="/incidencias/nueva" element={<NuevaIncidencia />} />
              <Route path="/incidencias/:id" element={<Incidencia />} />
              <Route path="/cierre" element={<CierreProyecto />} />
            </Route>
            <Route element={<RequiereSesion roles={['Analista']} />}>
              <Route path="/analista" element={<PanelAnalista />} />
              <Route path="/evaluacion" element={<Evaluacion />} />
              <Route path="/evaluacion/:id" element={<EvaluarProyecto />} />
              <Route path="/operaciones/compra" element={<Compra />} />
              <Route path="/operaciones/traslado" element={<Traslado />} />
              <Route path="/liquidaciones/:id" element={<Liquidacion />} />
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
