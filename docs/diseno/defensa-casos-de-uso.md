# Defensa del Diagrama de Casos de Uso del Sistema — Proyecto 11

Proyecto de Sistemas 1 · Los Cesaritos · 28/10/2026

**Dos diagramas** (en `04_Diagramas/`):
- **D1, según el docente** ([`01-casos-de-uso-sistema-v1-docente.mdj`](../diagramas/uml/01-casos-de-uso-sistema-v1-docente.mdj)): no tiene Usuario ni generalización, y el Administrador está conectado a los 17 CU.
- **D2, justificado** ([`01-casos-de-uso-sistema-v2-justificado.mdj`](../diagramas/uml/01-casos-de-uso-sistema-v2-justificado.mdj)): Usuario generaliza a Inversionista y Productor, y el Administrador solo está en Iniciar sesión, Gestionar usuarios y Consultar bitácora.

**Cambios en los dos:**
- Los CU siguen el orden del proceso, de arriba a abajo.
- Compra y traslado del ganado están separados.
- Postular y Evaluar incluyen «Verificar productor y predio».
- Expediente y portafolio están separados.
- Hay un «Gestionar usuarios, roles y permisos» (CRUD) y un «Consultar bitácora de auditoría» (RNF-03).

Todas las citas se verificaron en los PDF de `00_Material_Catedra/bibliografia_CU/`.

## Resumen

| Observación | ¿Tiene respaldo? | D1 | D2 |
|---|---|---|---|
| 1. «No existe la herencia» de actores | **No.** La norma la define y 6 libros la usan o la recomiendan. | Sin herencia | Con herencia |
| 2. El Administrador conectado a todo | **No.** Los ejemplos de los libros hacen lo contrario. | A todo | Solo a lo suyo |
| 3. Orden secuencial de arriba a abajo | **Es una buena práctica, no una regla.** | Sí | Sí |
| 4. Separar compra/traslado y relacionar postular/evaluar | **Sí, el docente tiene razón.** | Aplicado | Aplicado |

## 1. Herencia (generalización) entre actores

El docente dijo que está prohibida. **Ninguna fuente la prohíbe, y todas las que la tratan la permiten.**

- **OMG UML 2.5.1** (la norma oficial). El Actor es un Classifier (§18.2.1.3, p. 647), y los Classifiers admiten generalización: «Generalizations define generalization/specialization relationships between Classifiers» (§9.2.3.2, p. 100).
- **Booch, Rumbaugh y Jacobson** (*UML User Guide*, 2.ª ed., los creadores de la UML).
  - Cap. 16: «You can define general kinds of actors (such as Customer) and specialize them (such as CommercialCustomer) using generalization relationships».
  - Cap. 17: recomiendan, como paso para modelar el contexto, «Organize actors that are similar to one another in a generalization/specialization hierarchy».
  - **La Figura 17-2 tiene la misma estructura que nuestro D2.**
- **Rumbaugh et al.** (*Manual de referencia*, 2.ª ed., en español).
  - p. 69: «Los actores pueden ser definidos en jerarquías de generalización».
  - p. 262: la propia definición del diagrama incluye la «generalización entre los actores».
- **Kendall y Kendall** (*Análisis y diseño de sistemas*, 8.ª ed.).
  - p. 38: «Esta relación puede existir entre dos actores o dos casos de uso».
  - La Figura 2.15 la usa: «Orador principal» especializa a «Orador».
- **Dennis, Wixom y Tegarden** (5.ª ed., pp. 122-123): «The specialized actor inherits the behavior of the more general actor».
- **Ambler** (*Elements of UML 2.0 Style*, p. 44): las guías 81 y 82 explican cómo usarla («Is Like» Rule). «El Inversionista es como un Usuario» tiene sentido.
- **Cockburn** (*Writing Effective Use Cases*, p. 231): «it is official and correct».
- **Pressman** (7.ª ed.) **no la menciona** en todo el libro: ni la permite ni la prohíbe.
  - Él mismo dice que «el estudio exhaustivo del UML se deja a libros dedicados al tema» (p. 46).
  - Recomienda a Booch et al. y a Ambler (pp. 741-742), y los dos la usan.

## 2. «El Administrador debe estar conectado a todo»

**La línea entre un actor y un CU significa que el actor lo realiza, no que tenga acceso.**
- Pressman, p. 730: «Los actores se conectan mediante líneas a los casos de uso **que realizan**».
- Fowler, cap. 3: «Los actores llevan a cabo casos de uso».
- Booch, cap. 16: «the actor and the use case communicate with one another».

**En los ejemplos de los libros, el administrador solo está conectado a lo suyo:**
- **OMG Fig. 18.2 (p. 643):** en el ejemplo oficial del cajero automático, Administrator solo está en «Register ATM» y «Read Log». No está en retirar, transferir ni depositar.
- **Kendall Fig. 2.15 (p. 39):** el Presidente, que «planea y administra» la conferencia, está conectado solo a 2 CU.

**¿Quién corrige una cuenta duplicada o un error?** D2 lo resuelve sin conectar al Administrador a todo:
- **Gestionar usuarios, roles y permisos** (RF-06): alta, modificación, bloqueo, baja y corrección de cuentas duplicadas. Es el CRUD que el docente pidió.
- **Consultar bitácora de auditoría** (RNF-03). Nuestro requisito dice que «ninguna acción sensible se edita ni borra; una corrección se registra como nuevo evento». Es el equivalente a «Read Log» del ejemplo oficial.
- Si el Administrador estuviera conectado a Invertir o Liquidar, el diagrama diría que él invierte o liquida, y eso contradice RNF-03. Que pueda **ver** todo es un permiso de su rol, no una línea del diagrama.

## 3. Orden de arriba a abajo

- **OMG, Anexo B.1 (p. 686):** la posición de los elementos la controla el modelador. La notación de casos de uso no tiene ninguna regla de orden.
- **Ambler, guía 60 (pp. 34-35):** «Although diagrams should not reflect timing considerations […] you can increase the readability of your diagrams by arranging use cases to imply timing». Para el orden del proceso recomienda un diagrama de actividades.
- **Cockburn, guía 13 (p. 225):** pone arriba los objetivos de nivel más alto, no los que ocurren primero.

**Conclusión:** no es obligatorio, pero ayuda a leer el diagrama. Se aplicó en D1 y D2.

## 4. «include», «extend» y granularidad

**Cómo funcionan:**
- **«include»:** una parte común y **obligatoria**, que se ejecuta dentro del caso base, cada vez.
  - OMG, p. 641: «executed at a single location… before execution of the including UseCase is resumed».
  - Pressman, p. 730: el incluido es «uno de sus pasos».
  - Kendall, p. 37: es «comportamiento común para más de un caso de uso».
- **«extend»:** un agregado **opcional**, que se inserta en el caso base solo si se cumple una condición.
  - OMG, p. 640: «possibly conditionally… the extending UseCase typically defines behavior that may not necessarily be meaningful by itself».
  - Booch, cap. 16: «optional system behavior».
  - Kendall, p. 38: maneja «una variación o excepción».
  - Ambler, guía 73 (p. 41): usarlo con moderación.

**Prueba práctica:**

| Si… | Entonces | Ejemplo |
|---|---|---|
| B ocurre **siempre** dentro de A | A «include» B | Invertir «include» Validar OTP |
| B se agrega a A **a veces**, bajo una condición | B «extend» A | Reclamo al seguro «extend» Incidencias |
| B ocurre **después** que A, o lo hace otro actor | Ninguna relación: es una secuencia (BPMN o diagrama de actividades) | Postular → Evaluar; Compra → Traslado |

**Cómo aplica a nuestros CU:**
- **Postular y Evaluar no van con «include».**
  - Evaluar no ocurre dentro de Postular: lo hace otro actor (el Analista) y días después.
  - Postular se completa solo, cuando el proyecto queda «en revisión» (OMG p. 640: «This functionality must always be completed»).
  - Lo que comparten es la verificación del productor y el predio (RF-05). Por eso los dos «include» **Verificar productor y predio**.
- **Compra y traslado** son dos CU separados y sin relación: ocurren en momentos distintos.
- **Liquidar y Consultar expediente** quedan sin relación.
  - Hay un vínculo en los requisitos: RF-24 dice que «un reclamo vuelve al analista con la evidencia», y esa evidencia está en el expediente (RF-22).
  - Pero Consultar expediente tiene sentido por sí solo, y la norma dice que el que extiende «typically» no lo tiene.
  - Si el docente lo pide, la única forma correcta es **Consultar expediente --«extend»--> Liquidar proyecto**, con la condición [el inversionista quiere revisar la evidencia antes de dar conformidad o reclamar].
- **Expediente y portafolio** se separaron: tienen actores distintos (RF-22 vs RF-26).

## Para mostrar en la defensa
- Booch, Fig. 17-2.
- OMG, Fig. 18.2 (p. 643).
- Kendall, p. 38 y Fig. 2.15 (p. 39).
- Ambler, guía 60 (pp. 34-35).
- Pressman, pp. 46, 730 y 741-742.
