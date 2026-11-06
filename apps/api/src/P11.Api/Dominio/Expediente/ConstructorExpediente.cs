namespace P11.Api.Dominio.Expediente;

/// <summary>Tipo de evento de la línea de tiempo del expediente (RF-22).</summary>
public enum TipoEvento { Contrato, Compra, Traslado, Avance, Incidencia, Venta, Liquidacion }

public record ResumenFicha(Guid ProyectoId, string Nombre, string Linea, decimal MontoRequeridoBs, string Estado);
public record ReferenciaDocumento(Guid DocumentoId, Guid ProyectoId, string Tipo, DateTimeOffset Fecha);
public record EventoExpediente(Guid ProyectoId, TipoEvento Tipo, DateTimeOffset Fecha, string Descripcion);

/// <summary>Producto del Builder: una vista inmutable del expediente, con la cronología ordenada.</summary>
public sealed record VistaExpediente(
    ResumenFicha Ficha,
    IReadOnlyList<ReferenciaDocumento> Documentos,
    IReadOnlyList<EventoExpediente> Cronologia,
    bool EsVersionDeCierre);

public class ExpedienteIncompletoException(string mensaje) : InvalidOperationException(mensaje);

/// <summary>Interfaz del constructor: pasos de ensamblaje y una operación final.</summary>
public interface IConstructorExpediente
{
    IConstructorExpediente ConFicha(ResumenFicha ficha);
    IConstructorExpediente AgregarDocumento(ReferenciaDocumento documento);
    IConstructorExpediente AgregarEvento(EventoExpediente evento);
    IConstructorExpediente ComoVersionDeCierre();
    VistaExpediente Construir();
}

/// <summary>
/// Patrón Builder (GoF) para RF-22. Arma una vista del expediente a partir de registros que ya existen;
/// no persiste ni modifica evidencias (eso es de los servicios y de la bitácora, RNF-03).
/// Se usa un constructor nuevo por solicitud: no comparte estado entre construcciones.
/// </summary>
public sealed class ConstructorExpediente : IConstructorExpediente
{
    private ResumenFicha? _ficha;
    private readonly List<ReferenciaDocumento> _documentos = [];
    private readonly List<EventoExpediente> _eventos = [];
    private bool _cierre;
    private bool _construido;

    public IConstructorExpediente ConFicha(ResumenFicha ficha) { NoConstruido(); _ficha = ficha; return this; }
    public IConstructorExpediente AgregarDocumento(ReferenciaDocumento d) { NoConstruido(); _documentos.Add(d); return this; }
    public IConstructorExpediente AgregarEvento(EventoExpediente e) { NoConstruido(); _eventos.Add(e); return this; }
    public IConstructorExpediente ComoVersionDeCierre() { NoConstruido(); _cierre = true; return this; }

    public VistaExpediente Construir()
    {
        NoConstruido();
        if (_ficha is null) throw new ExpedienteIncompletoException("Falta la ficha del proyecto.");
        var id = _ficha.ProyectoId;

        var ajenoDoc = _documentos.FirstOrDefault(d => d.ProyectoId != id);
        if (ajenoDoc is not null) throw new ExpedienteIncompletoException($"El documento {ajenoDoc.DocumentoId} pertenece a otro proyecto.");
        var ajenoEv = _eventos.FirstOrDefault(e => e.ProyectoId != id);
        if (ajenoEv is not null) throw new ExpedienteIncompletoException($"El evento «{ajenoEv.Descripcion}» pertenece a otro proyecto.");

        // Una versión de cierre exige venta y liquidación; un expediente en curso puede omitirlas.
        if (_cierre)
            foreach (var obligatorio in new[] { TipoEvento.Venta, TipoEvento.Liquidacion })
                if (!_eventos.Any(e => e.Tipo == obligatorio))
                    throw new ExpedienteIncompletoException($"La versión de cierre exige el evento {obligatorio}.");

        _construido = true;
        return new VistaExpediente(
            _ficha,
            _documentos.OrderBy(d => d.Fecha).ToList().AsReadOnly(),
            _eventos.OrderBy(e => e.Fecha).ToList().AsReadOnly(),
            _cierre);
    }

    private void NoConstruido()
    {
        if (_construido) throw new InvalidOperationException("Este constructor ya entregó su expediente; cree uno nuevo por solicitud.");
    }
}
