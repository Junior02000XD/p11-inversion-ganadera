using System.Security.Claims;
using P11.Api.Data;
using P11.Api.Services.Files;

namespace P11.Api.Services.Documents;

public record ArchivoAbierto(string Nombre, string ContentType, byte[] Datos);

public interface IVisorDocumentos
{
    /// <summary>Devuelve el archivo descifrado, o null si no existe o quien lo pide no puede verlo.</summary>
    Task<ArchivoAbierto?> AbrirAsync(Guid documentoId, ClaimsPrincipal solicitante, CancellationToken ct = default);
}

/// <summary>Busca el documento y lo lee del almacén. No revisa permisos: eso lo hace VisorDocumentosProxy.</summary>
public class VisorDocumentos(AppDbContext db, IAlmacenArchivos almacen) : IVisorDocumentos
{
    public async Task<ArchivoAbierto?> AbrirAsync(Guid documentoId, ClaimsPrincipal solicitante, CancellationToken ct = default)
    {
        var documento = await db.Documentos.FindAsync([documentoId], ct);
        if (documento is null) return null;

        var datos = await almacen.LeerAsync(documento.ArchivoCifrado, ct);
        var tipo = string.IsNullOrEmpty(documento.ContentType) ? "application/octet-stream" : documento.ContentType;
        return new ArchivoAbierto(documento.NombreOriginal, tipo, datos);
    }
}
