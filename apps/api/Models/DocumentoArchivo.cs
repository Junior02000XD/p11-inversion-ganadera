namespace P11.Api.Models;

/// <summary>Archivo subido por un usuario (CI, selfie, RUNSA, título o contrato). En disco está cifrado (RNF-02).</summary>
public class DocumentoArchivo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public TipoDocumento Tipo { get; set; }
    public string NombreOriginal { get; set; } = "";
    public string ContentType { get; set; } = "";
    /// <summary>Nombre del archivo cifrado dentro de la carpeta de archivos.</summary>
    public string ArchivoCifrado { get; set; } = "";
    public DateTimeOffset SubidoEn { get; set; } = DateTimeOffset.UtcNow;
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Pendiente;
    public string? Motivo { get; set; }
    public string? RevisadoPor { get; set; }
    public DateTimeOffset? RevisadoEn { get; set; }

    /// <summary>Documentos del predio (RF-05); los demás tipos son de identidad (CI y selfie).</summary>
    public static readonly TipoDocumento[] TiposPredio = [TipoDocumento.Runsa, TipoDocumento.TituloPropiedad, TipoDocumento.ContratoAlquiler];
}
