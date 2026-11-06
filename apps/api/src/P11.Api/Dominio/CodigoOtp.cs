namespace P11.Api.Dominio;

/// <summary>Código de un solo uso enviado por correo (RNF-01). Solo se guarda su hash.</summary>
public class CodigoOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public PropositoOtp Proposito { get; set; }
    public string CodigoHash { get; set; } = "";
    public DateTimeOffset ExpiraEn { get; set; }
    public int Intentos { get; set; }
    public DateTimeOffset? UsadoEn { get; set; }
    public bool Anulado { get; set; }
}
