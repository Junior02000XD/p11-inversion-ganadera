using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace P11.Api.Configuracion;

/// <summary>
/// Completa el documento OpenAPI que leen Swagger UI (/swagger) y Scalar (/scalar): título, descripción
/// y el esquema Bearer, para que el botón «Authorize» acepte el token de <c>POST /api/auth/login</c>.
/// </summary>
public sealed class DocumentoOpenApi : IOpenApiDocumentTransformer
{
    public const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken ct)
    {
        documento.Info.Title = "P11 – Plataforma digital de inversión ganadera";
        documento.Info.Version = "v1";
        documento.Info.Description =
            "Web API del Proyecto 11 (.NET 10, EF Core y SQL Server). Recurso principal: `/api/proyectos` " +
            "(fichas de proyecto ganadero); recurso relacionado: `/api/proyectos/{id}/historial`. " +
            "Para probar: `POST /api/auth/login` con un usuario de la semilla, copiar `token` y pegarlo en «Authorize».";

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT que devuelve POST /api/auth/login (solo el token, sin «Bearer »).",
        };
        documento.Security ??= [];
        documento.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(Esquema, documento)] = [] });
        return Task.CompletedTask;
    }
}
