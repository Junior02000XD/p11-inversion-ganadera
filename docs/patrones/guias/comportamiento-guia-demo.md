# Guía de demo en vivo: patrones de comportamiento

Proyecto 11, Inversión Ganadera · Equipo Los Cesaritos · 13/11/2026
Documento base: [`../informes/comportamiento-informe.pdf`](../informes/comportamiento-informe.pdf)

Esta guía dice qué mostrar, en qué orden y qué decir de cada patrón. El reparto de quién habla está en la guía de estudio ([`comportamiento-guia-estudio.pdf`](comportamiento-guia-estudio.pdf)).

## Resumen: qué se muestra y cómo

| Patrón | Cómo se muestra | Archivo para abrir (en `apps/api/`) |
|---|---|---|
| State | En vivo (web) | `Models/Projects/States/EstadoFicha.cs` |
| Chain of Responsibility | En vivo (web) + prueba | `Services/Projects/RequisitosEnvio.cs` |
| Command | En vivo (web, historial) | `Services/Projects/ComandosProyecto.cs`, `GestorCicloProyecto.cs` |
| Observer | En vivo (Mailpit) | `Services/Projects/ObservadoresProyecto.cs`, `Program.cs` |
| Strategy | Prueba | `Models/Settlements/ReglaReparto.cs` |
| Template Method | Prueba (ya estaba) | `Models/Projects/ProyectoGanadero.cs` (`Validar`) |
| Iterator | Prueba (lo da C#) | `Models/Settlements/ConceptoLiquidacion.cs` (`Detalle`) |
| Mediator | Solo diagrama: no aplica | — |
| Memento | Solo diagrama: no aplica | — |

## 1. Preparación (antes de la clase, unos 10 minutos)

En una terminal, desde la raíz del repositorio `p11-inversion-ganadera`:

```bash
docker compose up -d --wait         # SQL Server (1433) y Mailpit (8025)
cd apps/api && dotnet run --launch-profile https   # API en https://localhost:7080
```

En otra terminal:

```bash
cd apps/web && npm run dev                             # web en http://localhost:5173
```

Abrir en el navegador:

1. **Pestaña A, ventana normal:** http://localhost:5173 → entrar como **analista** (`analista@p11.local` / `Analista.p11-2026`).
2. **Pestaña B, ventana privada:** http://localhost:5173 → entrar como **productor** Luis Pérez (`luis.perez.208686@demo.bo` / `Productor.2026`). Tiene que ser ventana privada porque la sesión se guarda por ventana.
3. **Pestaña C:** Mailpit, http://localhost:8025. Borrar los correos viejos (botón «Delete all») para que se vean solo los de la demo.
4. En el editor (VS Code), dejar abiertos los archivos de la tabla de arriba.

**Comprobar antes de empezar:** en la pestaña A, menú «Evaluación», tiene que aparecer «Engorde Chiquitania 2026» en «En revisión técnica». Si no aparece (porque se usó en un ensayo), en la pestaña B ir a «Mis proyectos» y apretar «Enviar a evaluación técnica».

## 2. El flujo en vivo (unos 3 minutos)

| # | Pestaña | Qué se hace | Qué se ve | Patrón que se nombra |
|---|---|---|---|---|
| 1 | A (analista) | Menú «Evaluación». | La lista con «Engorde Chiquitania 2026», productor Luis Pérez, «En revisión técnica». | (Datos reales de la API, ya no de ejemplo). |
| 2 | A | Apretar «Evaluar». Marcar solo «Riesgos identificados». Apretar «Aprobar y publicar». | Mensaje rojo: «No se publica un proyecto sin riesgos y tres escenarios cargados.». El proyecto no cambia. | **Command**: el comando `AprobarYPublicarProyecto` revisa el checklist (RF-10) antes de ejecutar. Nada queda en la bitácora. |
| 3 | A | Escribir en Observaciones: «Falta detallar la alimentación.» y apretar «Observar». | Vuelve a la lista con «"Engorde Chiquitania 2026" volvió al productor con observaciones.». El proyecto ya no está en la lista. | **Command** (`ObservarProyecto`) + **State**: `EnRevisionTecnica.Observar()` devuelve `EnBorrador`. |
| 4 | C (Mailpit) | Recargar. | Correo a Luis: «Tu proyecto «Engorde Chiquitania 2026»: tiene observaciones», con el texto. | **Observer**: `AvisarAlProductor` se enteró sin que el gestor lo conozca. El aviso sale por el Bridge de la exposición anterior. |
| 5 | B (productor) | Menú «Mis proyectos» (recargar). | El proyecto está en «Borrador» con un recuadro «Observaciones del analista: Falta detallar la alimentación.» y el botón «Enviar a evaluación técnica». | **State**: solo en Borrador se puede enviar (y editar). |
| 6 | B | Apretar «Enviar a evaluación técnica». | «Enviado a evaluación técnica.» y el estado pasa a «En revisión técnica». | **Chain of Responsibility**: pasan los tres eslabones (ficha completa → productor verificado → documentos del predio). **State**: Borrador → En revisión. |
| 7 | C (Mailpit) | Recargar. | Dos correos nuevos: «Proyecto por evaluar: «Engorde Chiquitania 2026»» (al analista) y «…: en evaluación técnica» (a Luis). | **Observer**: dos observadores, cada uno reacciona a lo suyo. |
| 8 | A (analista) | «Evaluación» → «Evaluar» otra vez y bajar hasta «Historial». | Tres filas: «Enviar a revisión», «Observar», «Enviar a revisión», cada una con fecha, motivo y responsable. | **Command**: la bitácora (`HistorialProyecto`, RNF-03) la escribe el invocador para todas las acciones. |

Al terminar, el proyecto queda otra vez en revisión, listo para el próximo ensayo.

**No apretar «Rechazar» ni aprobar con las dos casillas en la demo**: el proyecto quedaría Rechazado o En financiamiento, que son estados sin salida, y habría que crear otro proyecto para volver a mostrar el flujo. Si el docente lo pide, se puede hacer al final: el estado lo muestra la lista de «Mis proyectos» del productor.

## 3. Lo que se muestra con pruebas (unos 2 minutos)

En una terminal, desde la raíz del repo:

```bash
dotnet test tests/api --filter "FullyQualifiedName~Patterns"      # todas las pruebas de patrones (las tres familias)
```

Para cada patrón por separado (útil si preguntan por uno):

```bash
dotnet test tests/api --filter "FullyQualifiedName~StateTests"
dotnet test tests/api --filter "FullyQualifiedName~ChainOfResponsibilityTests"
dotnet test tests/api --filter "FullyQualifiedName~CommandTests"
dotnet test tests/api --filter "FullyQualifiedName~ObserverTests"
dotnet test tests/api --filter "FullyQualifiedName~StrategyTests"
dotnet test tests/api --filter "FullyQualifiedName~TemplateMethodTests"
dotnet test tests/api --filter "FullyQualifiedName~IteratorTests"
dotnet test tests/api --filter "FullyQualifiedName~EvaluacionProyectosTests"   # el flujo de la demo, de punta a punta
dotnet test tests/api                                   # todo: 112 pruebas
```

Qué decir de cada una, abriendo el archivo de la prueba en `tests/api/Patterns/`:

- **Chain of Responsibility.** `El_primero_que_falla_responde_y_los_siguientes_no_se_consultan`: la cadena corta en el eslabón que falla. `La_cadena_real_pide_verificacion_antes_que_documentos`: un productor sin verificar recibe «Tu identidad todavía no está verificada.» aunque también le falten documentos.
- **Strategy.** `Otra_regla_cambia_el_reparto_sin_cambiar_el_calculo_del_resultado`: con otra regla cambia lo que recibe cada inversionista, pero el resultado de la liquidación (62.500 Bs) es el mismo. La regla «partes iguales» existe solo en la prueba; la definitiva la decide el cliente (lo dice el ERS).
- **Template Method.** `Validar_aplica_las_reglas_comunes_y_despues_las_de_la_linea`: primero el error común (Nombre) y después los de lechería.
- **Iterator.** `Dos_recorridos_del_mismo_arbol_no_se_estorban`: cada `foreach` tiene su propio iterador, que genera C# a partir del `yield return`.
- **Observer.** `Todos_los_observadores_reciben_el_cambio_aunque_uno_falle`: si el correo se cae, la decisión del analista no se pierde.

## 4. Mediator y Memento (sin demo)

Se muestran con el diagrama del documento (secciones 5.8 y 5.9) y se explica por qué no se aplicaron:

- **Mediator:** los pasos de la inversión (reserva, contrato, pago, participación) van en secuencia y no se hablan entre sí. Alcanza con una Facade y con Observer.
- **Memento:** no hay requisito de deshacer (la evidencia no se borra, RNF-03); la historia está en la base y el simulador conserva la base con Prototype.

## 5. Si algo falla en vivo

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| La lista de Evaluación está vacía | El proyecto ya no está en revisión | Pestaña B → «Enviar a evaluación técnica». |
| «No se pudo conectar con el servidor.» | La API no está corriendo | Volver a correr `dotnet run --launch-profile https`. |
| No llegan correos a Mailpit | El contenedor está apagado | `docker compose up -d --wait`. |
| La sesión se cierra sola | El token venció | Volver a iniciar sesión. |
| Cualquier otra cosa | — | No improvisar: decir qué tendría que pasar y correr `dotnet test tests/api --filter "FullyQualifiedName~EvaluacionProyectosTests"`, que hace el mismo flujo. |
