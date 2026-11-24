# Guía de estudio del Proyecto 11

[`guia-de-estudio.html`](guia-de-estudio.html) explica el proyecto completo para estudiarlo en cualquier momento:

- qué resuelve y los cuatro roles;
- las tecnologías y por qué se eligieron;
- cómo viaja un pedido de la web a la base de datos;
- el mapa del repositorio y el código clave explicado;
- cada caso de uso con sus capturas, su flujo, las reglas de cada casilla y sus errores;
- las cifras clave, los patrones de diseño, las pruebas, cómo levantar la demo y preguntas para practicar.

Se abre con doble clic en cualquier navegador. Las capturas están en [`capturas/`](capturas/) y son de la aplicación real, con la API, SQL Server y Mailpit.

También está publicada en línea, para abrirla sin clonar el repositorio: [Guía del Proyecto 11](https://claude.ai/artifact/YAswddBodFh72SRUxs6HCc). Este HTML es la fuente y la versión en línea es una copia.

## Cómo actualizarla

1. Lo que cambia de sprint a sprint está en el bloque **«DATOS DE LA GUÍA»**, dentro del `<script>` al final del HTML:
   - el estado de cada sprint;
   - el estado de cada caso de uso: `'api'` si funciona con la base de datos, `'ejemplo'` si la pantalla todavía usa datos de ejemplo.
2. Para un caso de uso nuevo, copiar un bloque `cu-bloque` de la sección 7 y poner sus capturas en `capturas/`, numeradas.
3. Cada sección empieza con un recuadro «En breve» que repite lo mínimo para leerla sola. Mantenerlo al día si cambia algo de lo que resume.
4. Si cambia el código, revisar sobre todo estas secciones:
   - 3, tecnologías;
   - 5, mapa del repositorio;
   - 10, pruebas: cuántas hay y en qué archivos;
   - 11, cómo levantar la demo: `docker-compose.yml`, `.env` y la cadena de conexión.
5. Sumar una fila en la sección «Historial de la guía».
6. Volver a publicar la versión en línea **en la misma dirección**: no se crea una página nueva. Las capturas solo se vuelven a subir si cambian.
