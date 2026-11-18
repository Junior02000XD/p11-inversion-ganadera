using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using P11.Api.Datos;
using P11.Api.Dominio;

namespace P11.Api.Servicios.Documentos;

/// <summary>
/// Proxy de protección (RNF-02): tiene la misma interfaz que el visor real y se pone delante de él.
/// Solo deja abrir un documento a su dueño, a un analista o al administrador, y registra cada acceso.
/// La regla depende del documento, no solo del rol, por eso no alcanza con [Authorize(Roles = ...)].
/// </summary>
public class VisorDocumentosProxy(VisorDocumentos real, AppDbContext db, ILogger<VisorDocumentosProxy> log) : IVisorDocumentos
{
    public async Task<ArchivoAbierto?> AbrirAsync(Guid documentoId, ClaimsPrincipal solicitante, CancellationToken ct = default)
    {
        var dueno = await db.Documentos
            .Where(d => d.Id == documentoId)
            .Select(d => (Guid?)d.UsuarioId)
            .FirstOrDefaultAsync(ct);
        if (dueno is null) return null;

        if (!solicitante.IsInRole(Roles.Analista) && !solicitante.IsInRole(Roles.Administrador) && solicitante.IdUsuario() != dueno)
        {
            // Se responde igual que si no existiera, para no confirmar que el documento está ahí.
            log.LogWarning("{Correo} intentó abrir el documento {Documento}, que no es suyo", solicitante.Correo(), documentoId);
            return null;
        }

        log.LogInformation("{Correo} abrió el documento {Documento}", solicitante.Correo(), documentoId);
        return await real.AbrirAsync(documentoId, solicitante, ct);
    }
}
