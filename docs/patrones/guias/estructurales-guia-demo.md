# Guía de demo en vivo: patrones estructurales

Proyecto 11, Inversión Ganadera · Equipo Los Cesaritos
Documento base: [`../informes/estructurales-informe.pdf`](../informes/estructurales-informe.pdf)

Esta guía dice qué mostrar, en qué orden y qué decir de cada patrón. Casi todos se ven en un solo recorrido: un usuario nuevo que se registra, verifica su identidad y sube un documento.

## Resumen: qué se muestra y cómo

| Patrón | Cómo se muestra | Archivo para abrir (en `apps/api/src/P11.Api/`) |
|---|---|---|
| Adapter | En vivo: llega el código OTP a Mailpit | `Servicios/CorreoSmtpService.cs` |
| Decorator (cifrado) | En vivo: el archivo en disco no se puede leer, pero «Ver» lo abre | `Servicios/AlmacenDisco.cs`, `AlmacenCifrado.cs`, `Program.cs` |
| Decorator (reintentos) | En vivo, opcional: se apaga Mailpit y la API reintenta | `Servicios/CorreoConReintentos.cs` |
| Facade | En vivo: verificación de identidad | `Servicios/VerificacionService.cs` |
| Bridge | En vivo: avisos por correo; Telegram con prueba | `Servicios/Avisos/` |
| Proxy | En vivo, el dueño abre su documento; el acceso ajeno, con prueba | `Servicios/Documentos/VisorDocumentosProxy.cs` |
| Composite | Prueba (el endpoint llega en el Sprint 3) | `Dominio/Liquidaciones/ConceptoLiquidacion.cs` |
| Flyweight | Sin demo: no aplica, se defiende con números | — |

**Ojo:** la pantalla «Liquidaciones» de la web todavía muestra datos de ejemplo, no el Composite. No presentarla como si lo usara.

## 1. Preparación (antes de la clase)

```bash
docker compose up -d                                          # desde la raíz del repo
cd apps/api/src/P11.Api && dotnet run --launch-profile https  # API (dejar la terminal visible: ahí salen los registros)
cd apps/web && npm run dev                                    # web (otra terminal)
```

Preparar una carpeta en el escritorio con:

- **tres imágenes JPG o PNG** de más de 1 KB (sirven fotos cualquiera): `ci-frente.jpg`, `ci-dorso.jpg` y `selfie-revision.jpg`. El nombre de la selfie importa: si contiene «revision», la verificación simulada da una coincidencia baja y pasa a revisión manual del analista, que es el camino que más patrones muestra. Con otro nombre, queda «Verificado» directo;
- **un PDF cualquiera**, por ejemplo `runsa.pdf`.

Abrir:

1. **Pestaña A, ventana normal:** http://localhost:5173 para el usuario nuevo.
2. **Pestaña B, ventana privada:** http://localhost:5173 como **analista** (`analista@p11.local` / `Analista.p11-2026`).
3. **Pestaña C:** Mailpit, http://localhost:8025, con los correos viejos borrados («Delete all»).

Para cada ensayo hace falta un correo y un CI que no estén usados: por ejemplo `expo1@demo.bo` con CI `6100001`, después `expo2@demo.bo` con CI `6100002`, y así.

## 2. El recorrido en vivo (unos 5 minutos)

| # | Pestaña | Qué se hace | Qué se ve | Patrón que se nombra |
|---|---|---|---|---|
| 1 | A | «Crear cuenta». Nombre, correo `expo1@demo.bo`, celular `70011122`, CI `6100001`, contraseña de 8 o más caracteres (por ejemplo `Expo.2026`), «Soy»: Productor. Registrar. | Pasa a la pantalla para ingresar el código. | — |
| 2 | C | Recargar Mailpit. | Correo «Tu código de verificación: 123456». | **Adapter**: el registro llama a `ICorreoService.EnviarAsync(para, asunto, cuerpo)` y `CorreoSmtpService` lo traduce a `SmtpClient` y `MailMessage` de .NET. Ni el registro ni el OTP saben que existe SMTP. |
| 3 | A | Escribir el código y confirmar. Iniciar sesión. | Entra como Productor, con la identidad «Pendiente». | — |
| 4 | A | Menú «Inicio» → botón «Verificar identidad». Subir `ci-frente.jpg`, `ci-dorso.jpg` y `selfie-revision.jpg`, marcar el consentimiento y «Enviar para verificación». | Estado «En revisión». | **Facade**: el controlador llama solo a `VerificacionService.EnviarAsync`, que coordina cinco piezas: el almacén cifrado, el proveedor de identidad, el umbral, el historial y el aviso. |
| 5 | C | Recargar. | Correo «Verificación de identidad: En revisión». | **Bridge**: `AvisoVerificacion` (qué se dice) sale por `CanalCorreo` (por dónde). |
| 6 | B (analista) | Menú «Identidad y documentos». Aparece el usuario nuevo con sus tres imágenes. Abrir una con su enlace y apretar «Aprobar». | La imagen se ve; el usuario pasa a «Verificado». | **Proxy**: el analista puede abrir el archivo porque `VisorDocumentosProxy` lo deja pasar. **Facade**: `DecidirAsync`. |
| 7 | C | Recargar. | Correo «Verificación de identidad: Verificado». | **Bridge** otra vez: el mismo canal, otro aviso. |
| 8 | A | Menú «Documentos». Elegir el tipo «Registro RUNSA» y subir `runsa.pdf`. Apretar «Ver». | El PDF se abre en otra pestaña. | **Proxy**: el dueño abre su propio documento. |
| 9 | Terminal | Desde `apps/api/src/P11.Api`: `ls -t App_Data/archivos \| head -1` y después `xxd App_Data/archivos/<ese archivo> \| head -3`. | Bytes ilegibles: no aparece el `%PDF` del comienzo de un PDF. | **Decorator**: `AlmacenCifrado` cifra con AES-256-GCM y le pasa los bytes a `AlmacenDisco`, que solo escribe. «Ver» descifra al leer. |
| 10 | Terminal de la API | Mirar los registros. | Líneas como «… abrió el documento …». | **Proxy**: registra cada acceso. |

### Opcional: los reintentos del correo (Decorator), 1 minuto

1. `docker stop p11-mailpit`.
2. En la pestaña A (cerrar sesión antes), registrar otra cuenta (`expo2@demo.bo`, CI `6100002`).
3. En la terminal de la API aparecen avisos: «Falló el envío a … (intento 1 de 3); se reintenta» y «(intento 2 de 3)». La cuenta se crea igual.
4. `docker start p11-mailpit`, y en la pantalla del código apretar «Reenviar código»: el correo llega.

Qué decir: `CorreoConReintentos` envuelve al adaptador SMTP con la misma interfaz. Un corte breve ya no deja al usuario sin su código.

### Al final: bloqueo de cuenta (Bridge, aviso urgente), 1 minuto

Hacerlo **al final**, porque la cuenta queda bloqueada 15 minutos. Con `expo1@demo.bo`, cerrar sesión e intentar entrar cinco veces con una contraseña incorrecta.

- La pantalla avisa cuántos intentos quedan y, al quinto, «Tu cuenta quedó bloqueada por intentos fallidos. Vuelve a intentarlo a las HH:MM. Te avisamos por correo.».
- En Mailpit llega «Bloqueamos tu cuenta por seguridad».

Qué decir: `AvisoBloqueo` es otro aviso del mismo puente y está marcado como urgente. Por Telegram llegaría con sonido; eso lo muestra la prueba de la sección 3.

**No hacer el bloqueo con las cuentas de la demo de comportamiento** (Luis o el analista).

## 3. Lo que se muestra con pruebas

Desde `apps/api`:

```bash
dotnet test --filter "FullyQualifiedName~AdapterTests"
dotnet test --filter "FullyQualifiedName~DecoratorTests"
dotnet test --filter "FullyQualifiedName~ProxyTests"
dotnet test --filter "FullyQualifiedName~BridgeTests"
dotnet test --filter "FullyQualifiedName~CompositeTests"
dotnet test --filter "FullyQualifiedName~VerificacionTests"   # la Facade, de punta a punta
```

Qué decir, abriendo las pruebas en `apps/api/tests/P11.Tests/Patrones/`:

- **Proxy.** `Otro_productor_no_puede_abrirlo_ni_saber_que_existe`: otro productor recibe «no encontrado», como si el documento no existiera, y el intento queda registrado. Esto no se muestra en vivo porque necesita el identificador del documento y el token de otra cuenta.
- **Bridge.** `El_mismo_aviso_sale_por_Telegram_y_suena_porque_es_urgente`: el mismo `AvisoBloqueo` sale por `CanalTelegram` sin cambiar su código. Telegram está programado y probado, pero no activado, porque los usuarios todavía no tienen un chat vinculado.
- **Decorator.** `Lee_los_archivos_guardados_antes_de_separar_el_cifrado`: los archivos subidos antes del cambio se siguen leyendo. `Se_rinde_despues_del_ultimo_intento`: después de tres intentos, el error llega a quien llamó.
- **Composite.** `Un_grupo_suma_tambien_los_grupos_que_contiene`: Costos (62.000 Bs) incluye Sanidad, que a su vez incluye Vacunas y Veterinario. `El_detalle_respeta_la_jerarquia`: el detalle sale con sangría por nivel, como en el informe de cierre.

## 4. Flyweight (sin demo)

Se explica con la tabla de la sección 5.7 del documento: compartir los datos de la raza ahorraría unos 100 bytes por animal, entre 30 KB y 1 MB en los casos realistas. Un celular le da a cada app entre 128 y 256 MB, y la base ya guarda la raza una sola vez. No aplica.

## 5. Si algo falla en vivo

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| «Ya existe una cuenta con ese correo» o «con ese CI» | Se usó en un ensayo | Usar el siguiente número (`expo3@demo.bo`, CI `6100003`). |
| «Algunas imágenes no se pueden leer» | Archivo que no es JPG ni PNG, o de menos de 1 KB | Usar otra foto. |
| Queda «Verificado» directo en vez de «En revisión» | La selfie no tiene «revision» en el nombre | Sirve igual para la Facade; el paso 6 del analista se salta. |
| No llega el código | Mailpit apagado | `docker start p11-mailpit` y «Reenviar código». |
| Cualquier otra cosa | — | Correr la prueba del patrón (sección 3). |
