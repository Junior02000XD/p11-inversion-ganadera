import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router'
import { IrAlInicio, Marco, RequiereSesion, VistaPorRol } from './componentes'
import { rolesDe } from './casosDeUso'
import { ANALISTA, INVERSIONISTA, PRODUCTOR } from './roles'
import { ProveedorSesion } from './sesion'
import Login from './paginas/Login'
import Registro from './paginas/Registro'
import VerificarCuenta from './paginas/VerificarCuenta'
import Identidad from './paginas/Identidad'
import { RevisionIdentidad, RevisionPredio } from './paginas/RevisionesAnalista'
import AdminUsuarios from './paginas/AdminUsuarios'
import Bitacora from './paginas/Bitacora'
import NuevoProyecto from './paginas/NuevoProyecto'
import MisProyectos from './paginas/MisProyectos'
import Documentos from './paginas/Documentos'
import Evaluacion from './paginas/Evaluacion'
import EvaluarProyecto from './paginas/EvaluarProyecto'
// Desde aquí, pantallas de los sprints 2 y 3 (por ahora con datos de ejemplo, ver datosEjemplo.js)
import Catalogo from './paginas/Catalogo'
import Simulador from './paginas/Simulador'
import Inversiones from './paginas/Inversiones'
import Invertir from './paginas/Invertir'
import Contrato from './paginas/Contrato'
import PagoQr from './paginas/PagoQr'
import Rondas from './paginas/Rondas'
import Ronda from './paginas/Ronda'
import Compra from './paginas/Compra'
import Traslado from './paginas/Traslado'
import Avance from './paginas/Avance'
import AvancesPendientes from './paginas/AvancesPendientes'
import AvancesRecibidos from './paginas/AvancesRecibidos'
import Incidencias from './paginas/Incidencias'
import NuevaIncidencia from './paginas/NuevaIncidencia'
import Incidencia from './paginas/Incidencia'
import Reclamos from './paginas/Reclamos'
import CierreProyecto from './paginas/CierreProyecto'
import MiLiquidacion from './paginas/MiLiquidacion'
import Liquidacion from './paginas/Liquidacion'
import Expedientes from './paginas/Expedientes'
import Expediente from './paginas/Expediente'
import Portafolio from './paginas/Portafolio'
import './estilos.css'
import { aplicarEstilo, estiloGuardado } from './estilo'

// El estilo elegido se aplica antes de pintar, para que no parpadee.
aplicarEstilo(estiloGuardado())

/** Rutas de un caso de uso: solo entran sus actores y el Administrador (casosDeUso.js). */
const CU = (id, rutas) => (
  <Route key={id} element={<RequiereSesion roles={rolesDe(id)} />}>
    {rutas}
  </Route>
)

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <ProveedorSesion>
      <BrowserRouter>
        <Routes>
          <Route element={<Marco />}>
            {/* Zona pública: registrarse e iniciar sesión */}
            <Route path="/ingresar" element={<Login />} />
            <Route path="/registro" element={<Registro />} />
            <Route path="/verificar-cuenta" element={<VerificarCuenta />} />

            {/* Una pestaña por caso de uso (diagrama D1). */}
            {CU('identidad', [
              <Route
                key="i"
                path="/identidad"
                element={
                  <VistaPorRol
                    vistas={[
                      {
                        roles: [INVERSIONISTA, PRODUCTOR],
                        etiqueta: 'Solicitante',
                        elemento: <Identidad />,
                        admin: false,
                      },
                      {
                        roles: [ANALISTA],
                        etiqueta: 'Analista',
                        elemento: <RevisionIdentidad />,
                      },
                    ]}
                  />
                }
              />,
            ])}
            {CU('usuarios', [<Route key="u" path="/admin/usuarios" element={<AdminUsuarios />} />])}
            {CU('bitacora', [<Route key="b" path="/admin/bitacora" element={<Bitacora />} />])}
            {CU('postular', [
              <Route key="p" path="/proyectos" element={<MisProyectos />} />,
              <Route key="pn" path="/proyectos/nuevo" element={<NuevoProyecto />} />,
              <Route key="pe" path="/proyectos/:id/editar" element={<NuevoProyecto />} />,
              <Route key="pd" path="/proyectos/documentos" element={<Documentos />} />,
            ])}
            {CU('evaluar', [
              <Route
                key="e"
                path="/evaluacion"
                element={
                  <div className="pila">
                    <Evaluacion />
                    <RevisionPredio />
                  </div>
                }
              />,
              <Route key="ei" path="/evaluacion/:id" element={<EvaluarProyecto />} />,
            ])}
            {CU('catalogo', [<Route key="c" path="/catalogo" element={<Catalogo />} />, <Route key="ci" path="/catalogo/:id" element={<Simulador />} />])}
            {CU('invertir', [
              <Route key="v" path="/inversiones" element={<Inversiones />} />,
              <Route key="vi" path="/inversiones/:id" element={<Invertir />} />,
              <Route key="vc" path="/inversiones/:id/contrato" element={<Contrato />} />,
              <Route key="vp" path="/inversiones/:id/pago" element={<PagoQr />} />,
            ])}
            {CU('ronda', [<Route key="r" path="/rondas" element={<Rondas />} />, <Route key="ri" path="/rondas/:id" element={<Ronda />} />])}
            {CU('compra', [<Route key="co" path="/compra" element={<Compra />} />])}
            {CU('traslado', [<Route key="t" path="/traslado" element={<Traslado />} />])}
            {CU('avance', [
              <Route
                key="a"
                path="/avance"
                element={
                  <VistaPorRol
                    vistas={[
                      {
                        roles: [PRODUCTOR],
                        etiqueta: 'Productor',
                        elemento: <Avance />,
                      },
                      {
                        roles: [ANALISTA],
                        etiqueta: 'Analista',
                        elemento: <AvancesRecibidos />,
                      },
                    ]}
                  />
                }
              />,
              <Route key="ap" path="/avance/pendientes" element={<AvancesPendientes />} />,
            ])}
            {CU('incidencias', [
              <Route key="n" path="/incidencias" element={<Incidencias />} />,
              <Route key="nn" path="/incidencias/nueva" element={<NuevaIncidencia />} />,
              <Route key="ni" path="/incidencias/:id" element={<Incidencia />} />,
            ])}
            {CU('reclamos', [<Route key="s" path="/reclamos" element={<Reclamos />} />])}
            {CU('liquidar', [
              <Route
                key="l"
                path="/liquidacion"
                element={
                  <VistaPorRol
                    vistas={[
                      {
                        roles: [PRODUCTOR],
                        etiqueta: 'Productor',
                        elemento: <CierreProyecto />,
                      },
                      {
                        roles: [ANALISTA],
                        etiqueta: 'Analista',
                        elemento: <Liquidacion />,
                      },
                      {
                        roles: [INVERSIONISTA],
                        etiqueta: 'Inversionista',
                        elemento: <MiLiquidacion />,
                      },
                    ]}
                  />
                }
              />,
            ])}
            {CU('expediente', [
              <Route key="x" path="/expedientes" element={<Expedientes />} />,
              <Route key="xi" path="/expedientes/:id" element={<Expediente />} />,
            ])}
            {CU('portafolio', [<Route key="f" path="/portafolio" element={<Portafolio />} />])}

            <Route path="/inicio" element={<IrAlInicio />} />
            <Route path="*" element={<IrAlInicio />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ProveedorSesion>
  </StrictMode>,
)
