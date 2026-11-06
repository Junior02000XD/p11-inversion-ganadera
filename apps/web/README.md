# Cliente web — React + Vite + TypeScript

Consume la API (`apps/api`). Requiere Node.js 20.19 o superior. En desarrollo, Vite reenvía `/api` a https://localhost:7080, así que no hace falta configurar CORS.

```bash
cd apps/web
npm install
npm run dev      # http://localhost:5173 (con la API corriendo)
npm run build    # verificación de tipos + build de producción en dist/
```

Para apuntar a otra API: `API_URL=https://otra-url npm run dev`.

## Pantallas (Sprint 1, según los mockups 01a–03b)
| Ruta | Pantalla | Rol |
|---|---|---|
| `/registro` | Crear cuenta (RF-01) | — |
| `/verificar-cuenta` | Código OTP (RNF-01) | — |
| `/ingresar` | Iniciar sesión, intentos restantes y bloqueo (RF-02) | — |
| `/inicio` | Inicio según rol (RF-06) | todos |
| `/identidad` | Verificación de identidad con consentimiento (RF-03/04, RNF-07) | Inversionista, Productor |
| `/proyectos/nuevo`, `/proyectos` | Ficha por línea en borrador y envío a evaluación (RF-07, RF-05) | Productor |
| `/documentos` | Documentos del predio (RF-05) | Productor |
| `/analista` | Revisión manual de identidad y documentos | Analista |
| `/admin/usuarios` | Usuarios y roles, desbloqueo (RF-06) | Administrador |

El token se guarda en `sessionStorage`: sobrevive a recargar la página, pero se pierde al cerrar la pestaña.
