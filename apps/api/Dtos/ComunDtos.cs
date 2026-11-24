namespace P11.Api.Dtos;

// DTO compartidos por varios controladores.
//
// Convención de esta carpeta (la misma del proyecto de clase CarritoComprasApi):
//   - Lo que ENTRA a la API (cuerpo de POST/PUT) es una class con { get; set; } y sus validaciones
//     arriba de cada propiedad. ASP.NET la arma desde el JSON y la valida antes de llamar al controlador.
//   - Lo que SALE de la API es un record: datos de solo lectura que se crean una vez y se devuelven.

/// <summary>Respuesta simple con un texto para mostrar al usuario.</summary>
public record Respuesta(string Mensaje);
