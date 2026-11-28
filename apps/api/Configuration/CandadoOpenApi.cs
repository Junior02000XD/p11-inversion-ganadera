using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace P11.Api.Configuration;

/// <summary>
/// Pone el candado (requisito Bearer) solo en las operaciones con <c>[Authorize]</c> y sin
/// <c>[AllowAnonymous]</c>, así Swagger muestra como públicos el login y el registro.
/// </summary>
public sealed class CandadoOpenApi : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operacion, OpenApiOperationTransformerContext contexto, CancellationToken ct)
    {
        var metadatos = contexto.Description.ActionDescriptor.EndpointMetadata;
        if (!metadatos.OfType<IAuthorizeData>().Any() || metadatos.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operacion.Security ??= [];
        operacion.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(DocumentoOpenApi.Esquema, contexto.Document)] = [],
        });
        return Task.CompletedTask;
    }
}
