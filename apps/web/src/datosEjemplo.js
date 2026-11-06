// Datos de ejemplo para las pantallas de los sprints 2 y 3.
// Esas pantallas todavía no tienen endpoints en la API: leen de acá.
// Cuando el endpoint exista, la pantalla cambia este import por una llamada a `api(...)`, como en el Sprint 1.

export const proyectos = [
  {
    id: 'chiquitania',
    nombre: 'Engorde Chiquitania 2026',
    linea: 'Engorde',
    ubicacion: 'San Ignacio de Velasco, Santa Cruz',
    riesgo: 'Medio',
    metaBs: 100000,
    recaudadoBs: 40000,
    minimoBs: 100,
    plazoMeses: 10,
    cierre: '30/11/2026',
    financiado: true, // cómo cierra la ronda en el ejemplo
    // Cuánto vuelve por cada boliviano invertido en cada escenario (RF-12).
    escenarios: { Pesimista: 0.9, Esperado: 1.1, Optimista: 1.25 },
    riesgos: 'Pérdida de ganado, enfermedades o caída del precio de venta. La rentabilidad no está garantizada.',
  },
  {
    id: 'cotoca',
    nombre: 'Lechería Cotoca',
    linea: 'Lecheria',
    ubicacion: 'Cotoca, Santa Cruz',
    riesgo: 'Medio',
    metaBs: 80000,
    recaudadoBs: 52000,
    minimoBs: 100,
    plazoMeses: 12,
    cierre: '15/12/2026',
    financiado: true,
    escenarios: { Pesimista: 0.92, Esperado: 1.08, Optimista: 1.18 },
    riesgos: 'Baja en la producción de leche, enfermedades del hato o caída del precio del litro. La rentabilidad no está garantizada.',
  },
  {
    id: 'warnes',
    nombre: 'Lechería Warnes',
    linea: 'Lecheria',
    ubicacion: 'Warnes, Santa Cruz',
    riesgo: 'Bajo',
    metaBs: 60000,
    recaudadoBs: 21000,
    minimoBs: 100,
    plazoMeses: 12,
    cierre: '10/12/2026',
    financiado: false,
    escenarios: { Pesimista: 0.95, Esperado: 1.06, Optimista: 1.12 },
    riesgos: 'Baja en la producción de leche o caída del precio del litro. La rentabilidad no está garantizada.',
  },
]

export const buscarProyecto = (id) => proyectos.find((p) => p.id === id) ?? proyectos[0]

// Cola del analista (RF-09).
export const porEvaluar = [
  { id: 'chiquitania', nombre: 'Engorde Chiquitania 2026', productor: 'Juan Pérez', estado: 'En revisión' },
  { id: 'cotoca', nombre: 'Lechería Cotoca', productor: 'Ana Suárez', estado: 'En revisión' },
  { id: 'montero', nombre: 'Engorde Montero', productor: 'Luis Rojas', estado: 'Observado' },
]

// Portafolio del inversionista de ejemplo (RF-26).
export const portafolio = [
  { proyectoId: 'chiquitania', montoBs: 1000, estado: 'Activa' },
  { proyectoId: 'cotoca', montoBs: 500, estado: 'Activa' },
]

// Expediente del proyecto en ejecución (RF-22).
export const expediente = {
  estado: 'En ejecución',
  incidenciasAbiertas: 1,
  avances: [
    { fecha: '12/10/2026', detalle: '118 cabezas · 340 kg promedio' },
    { fecha: '05/10/2026', detalle: '119 cabezas · 326 kg promedio' },
    { fecha: '28/09/2026', detalle: '120 cabezas · 311 kg promedio' },
  ],
  documentos: ['Contrato de inversión', 'Compra: 120 cabezas a Hacienda San Antonio', 'Guía GMA-000123', 'Registro RUNSA'],
}

// Avances guardados en el teléfono del productor (RF-18, RNF-04).
export const avancesGuardados = [
  { fecha: '12/10', estado: 'Pendiente' },
  { fecha: '05/10', estado: 'Enviado' },
]

// Cierre del proyecto (RF-23).
export const liquidacion = {
  proyectoId: 'chiquitania',
  ventaBs: 130000,
  costosBs: 62000,
  perdidasBs: 3000,
  comisionBs: 2500,
}
export const resultadoLiquidacion = (l) => l.ventaBs - l.costosBs - l.perdidasBs - l.comisionBs
