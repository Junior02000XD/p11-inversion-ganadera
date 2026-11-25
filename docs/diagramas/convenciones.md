# Convenciones de los diagramas

Base común para todos los diagramas: vienen de los RF/RNF de la [ERS 2.0](../requisitos/ers-v2.0.pdf). Si alguien cree que algo de esta página tiene que cambiar, lo avisa al grupo **antes** de usarlo en un diagrama.

## Los 12 casos de uso

Derivados de RF-01…RF-26 (RF-27, mercado secundario, queda fuera). Un CU agrupa los RF que sirven al mismo objetivo de un actor. Hay dos «include» transversales:
- **Validar OTP** (RNF-01), en CU-01, CU-06 y CU-12;
- **Notificar** (RF-04, 15, 17, 20, 24, 25).

| CU | Nombre | RF | RNF | Actores |
|---|---|---|---|---|
| CU-01 | Registrar cuenta y verificar identidad | 01, 03, 04 | 01, 02, 07 | Usuario (Inversionista / Productor), Analista (revisión manual) |
| CU-02 | Iniciar sesión y acceder según rol | 02, 06 | 01, 03 | Usuario, Administrador (asigna rol y permisos) |
| CU-03 | Postular proyecto | 05 (carga de documentos), 07, 08 | 04 | Productor |
| CU-04 | Evaluar y publicar proyecto | 05 (revisión), 09, 10 | 06 | Analista |
| CU-05 | Explorar catálogo y simular escenarios | 11, 12 | 06, 10 | Inversionista |
| CU-06 | Invertir en un proyecto | 13, 14, 15, 16 | 01, 05, 09 | Inversionista (la pasarela de pago es un servicio externo, no un actor) |
| CU-07 | Cerrar ronda de financiamiento (Should) | 17 | 09 | Inversionista y Productor (reciben el resultado; lo dispara la fecha de cierre, sin actor) |
| CU-08 | Registrar avance productivo | 18, 19 (Should) | 04, 11 | Productor, Analista |
| CU-09 | Registrar compra y traslado del ganado | derivado del BPMN (sin RF propio) | 11 | Operaciones/Analista (un solo actor) |
| CU-10 | Gestionar incidencias | 20, 21 (Could) | 11 | Productor, Analista (la aseguradora es externa, no es actor) |
| CU-11 | Consultar expediente y portafolio | 22, 26 (Should) | 10 | Inversionista (el expediente también lo ven el Analista y el Productor) |
| CU-12 | Liquidar proyecto | 23, 24, 25 + carga de venta y costos (sin RF propio) | 01, 09 | Productor (carga la factura de venta y los costos), Analista, Inversionista |

**Supuestos declarados** (huecos de la especificación; no inventar más):
- Compra, traslado con GMA, y venta y costos aparecen en el BPMN y en RF-22, RF-23 y RNF-11, pero no tienen RF propio. Se proponen como RF-28 (compra y traslado) y RF-29 (venta y costos); hoy son requerimientos derivados.
- El Administrador solo existe en RF-06: asigna rol y permisos (CU-02).
- Las notificaciones no tienen RF propio; se modelan como acción del Sistema («include»).

## Actores

Solo son actores quienes inician sesión en el sistema:
- **Usuario**, que generaliza a Inversionista y Productor;
- **Inversionista**;
- **Productor**;
- **Analista/Operaciones** (un solo actor);
- **Administrador**.

Pasarela de pago, Aseguradora, SENASAG, ASFI y el reloj del sistema (reserva de 30 min, fecha de cierre) **no son actores**: se mencionan en el flujo, pero no se dibujan como actores.

En el código no hay subclases por rol: `Usuario` es una sola clase y el rol es un dato (`Rol`: Inversionista, Productor, Analista, Administrador). Solo Inversionista y Productor se registran solos; los roles Analista y Administrador vienen de la semilla de datos o los asigna el Administrador (RF-06).

## Cómo aparece el Sistema en cada diagrama

| Diagrama | Sistema |
|---|---|
| Actividad | **Como carril propio** (regla del docente). |
| Casos de uso | No es actor: es el límite del sistema, el recuadro que contiene los CU. Los actores quedan fuera. |
| Secuencia / Colaboración | Como líneas de vida por componente, no como un único «Sistema»: **App (Android/web)** y **Servidor de aplicación**, más objetos de dominio (`:ProyectoGanadero`…) y sistemas externos (Pasarela de pago, Servicio de correo electrónico, Proveedor de verificación de identidad). Las dos usan los mismos mensajes y la misma numeración. |
| Clases, Objetos, Estados, Componentes, Despliegue | No aparece: modelan la estructura o el comportamiento interno del sistema. |

## Nombres comunes

Usar exactamente estos nombres en todos los diagramas:
- *Servidor de aplicación* (en Componentes es «API Gateway / Backend»);
- *Pasarela de pago (QR / transferencia)*;
- *Servicio de correo electrónico (OTP y avisos)*;
- *Proveedor de verificación de identidad*;
- *Aseguradora*.

Las clases tienen el nombre que tienen en el código, en español y en PascalCase (`ProyectoGanadero`, `HistorialProyecto`, `ConfiguracionEscenario`…).

Estados:
- **EST-3, ciclo del proyecto:** Borrador, En revisión técnica, Rechazado, En financiamiento. Los cuatro ya están en el código. Después vienen Cerrado sin fondos, En compra y traslado, En ejecución, Vendido, En liquidación y Liquidado.
- **EST-4, participación:** Reservada, Firmada, Pagada, Vencida y Rechazada, con los mismos nombres que `enum EstadoInversion` (`Models/Investments/Inversion.cs`). Después vienen Aportes devueltos y Liquidada (Sprint 3).

El OTP se usa en CU-01, CU-06 y CU-12. El inicio de sesión (CU-02) no lo usa: RF-02 bloquea la cuenta 15 minutos al 5.º intento fallido.

**Vocabulario:** proyecto, ronda, cupo, participación, expediente digital, escenario (pesimista/esperado/optimista), liquidación (venta − costos − pérdidas − comisión), incidencia, portafolio, GMA, RUNSA. Nunca escribir «rentabilidad garantizada».

## Reglas

- **Regla del docente:** cada acción del usuario lleva a una acción del sistema que valida, y esa acción a una compuerta que decide. Hay que incluir los caminos de falla.
- IDs `RF-xx` / `RNF-xx` tal como están en la ERS. Las etiquetas de trazabilidad van solo en las versiones de [`trazabilidad/`](trazabilidad/).
- Herramienta: StarUML (`.mdj`). Idioma: español.
