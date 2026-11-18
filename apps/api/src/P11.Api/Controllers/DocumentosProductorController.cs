using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P11.Api.Configuracion;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Servicios;
using P11.Api.Servicios.Documentos;

namespace P11.Api.Controllers;

/// <summary>RF-05: el productor carga el RUNSA y el título de propiedad o el contrato de alquiler del predio.</summary>
[ApiController]
[Route("api/productor/documentos")]
[Authorize(Roles = $"{Roles.Productor},{Roles.Administrador}")]
public class DocumentosProductorController(AppDbContext db, IAlmacenArchivos almacen, IVisorDocumentos visor, IOptions<ArchivosOptions> opciones) : ControllerBase
{
    private static readonly TipoDocumento[] TiposPredio = [TipoDocumento.Runsa, TipoDocumento.TituloPropiedad, TipoDocumento.ContratoAlquiler];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentoDto>>> Mios(CancellationToken ct)
    {
        // El administrador ve los documentos del predio de todos los productores.
        var id = User.IdUsuario();
        var todos = User.IsInRole(Roles.Administrador);
        var documentos = await db.Documentos
            .Where(d => (todos || d.UsuarioId == id) && TiposPredio.Contains(d.Tipo))
            .OrderByDescending(d => d.SubidoEn)
            .ToListAsync(ct);
        return documentos.Select(DocumentoDto.Desde).ToList();
    }

    [HttpGet("{id:guid}/archivo")]
    public async Task<IActionResult> Archivo(Guid id, CancellationToken ct)
    {
        var archivo = await visor.AbrirAsync(id, User, ct);
        return archivo is null ? NotFound() : File(archivo.Datos, archivo.ContentType, archivo.Nombre);
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<DocumentoDto>> Subir([FromForm] TipoDocumento tipo, IFormFile? archivo, CancellationToken ct)
    {
        var maximoMb = opciones.Value.TamanoMaximoBytes / 1_000_000;
        if (!TiposPredio.Contains(tipo))
            return BadRequest(new Respuesta("Ese tipo de documento no corresponde al predio."));
        if (archivo is null || archivo.Length == 0 || archivo.Length > opciones.Value.TamanoMaximoBytes)
            return BadRequest(new Respuesta($"Adjunta un archivo PDF, JPG o PNG de hasta {maximoMb} MB."));

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms, ct);
        var datos = ms.ToArray();
        if (!ValidadorArchivos.EsPdfOImagen(datos))
            return BadRequest(new Respuesta("Formato no admitido. Usa PDF, JPG o PNG."));

        var documento = new DocumentoArchivo
        {
            UsuarioId = User.IdUsuario(),
            Tipo = tipo,
            NombreOriginal = Path.GetFileName(archivo.FileName),
            ContentType = archivo.ContentType,
            ArchivoCifrado = await almacen.GuardarAsync(datos, ct),
        };
        db.Documentos.Add(documento);
        await db.SaveChangesAsync(ct);
        return Created("/api/productor/documentos", DocumentoDto.Desde(documento));
    }
}
