using Microsoft.EntityFrameworkCore;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Projects;

namespace P11.Api.Services.Projects;

/// <summary>
/// Chain of Responsibility (GoF): cada eslabón revisa un requisito para enviar la ficha a evaluación
/// técnica. Si no se cumple, responde con el motivo y la cadena se corta; si se cumple, pasa al siguiente.
/// </summary>
public abstract class RequisitoEnvio
{
    private RequisitoEnvio? _siguiente;

    /// <summary>Engancha el siguiente eslabón y lo devuelve, para encadenar: a.Luego(b).Luego(c).</summary>
    public RequisitoEnvio Luego(RequisitoEnvio siguiente)
    {
        _siguiente = siguiente;
        return siguiente;
    }

    /// <summary>Devuelve el motivo del primer requisito que no se cumple, o null si se cumplen todos.</summary>
    public async Task<string?> RevisarAsync(ProyectoGanadero proyecto, CancellationToken ct)
    {
        var incumplido = await IncumplidoAsync(proyecto, ct);
        if (incumplido is not null || _siguiente is null) return incumplido;
        return await _siguiente.RevisarAsync(proyecto, ct);
    }

    protected abstract Task<string?> IncumplidoAsync(ProyectoGanadero proyecto, CancellationToken ct);
}

/// <summary>RF-08: una ficha incompleta o fuera de rango no se envía.</summary>
public class FichaCompleta : RequisitoEnvio
{
    protected override Task<string?> IncumplidoAsync(ProyectoGanadero proyecto, CancellationToken ct)
    {
        var errores = proyecto.Validar();
        return Task.FromResult(errores.Count == 0 ? null : "La ficha tiene campos con error: " + string.Join(" ", errores.Select(e => e.Mensaje)));
    }
}

/// <summary>RF-04: un usuario no verificado no puede publicar.</summary>
public class ProductorVerificado(AppDbContext db) : RequisitoEnvio
{
    protected override async Task<string?> IncumplidoAsync(ProyectoGanadero proyecto, CancellationToken ct)
    {
        var productor = await db.Users.FindAsync([proyecto.ProductorId], ct);
        return productor?.EstadoVerificacion == EstadoVerificacion.Verificado ? null : "Tu identidad todavía no está verificada.";
    }
}

/// <summary>RF-05: sin el RUNSA y el título o el contrato del predio aprobados, el proyecto no pasa a revisión técnica.</summary>
public class DocumentosDelPredio(AppDbContext db) : RequisitoEnvio
{
    protected override async Task<string?> IncumplidoAsync(ProyectoGanadero proyecto, CancellationToken ct)
    {
        var aprobados = await db.Documentos
            .Where(d => d.UsuarioId == proyecto.ProductorId && d.Estado == EstadoDocumento.Aprobado)
            .Select(d => d.Tipo)
            .ToListAsync(ct);
        var tieneRunsa = aprobados.Contains(TipoDocumento.Runsa);
        var tienePredio = aprobados.Contains(TipoDocumento.TituloPropiedad) || aprobados.Contains(TipoDocumento.ContratoAlquiler);
        return tieneRunsa && tienePredio ? null : "Faltan documentos aprobados: el RUNSA y el título de propiedad o el contrato de alquiler.";
    }
}

/// <summary>
/// Arma la cadena en un solo lugar. El orden va de lo más barato a lo más caro: la ficha se revisa en memoria,
/// el productor es una consulta por clave y los documentos, una consulta con filtro.
/// </summary>
public class RequisitosEnvio(AppDbContext db)
{
    public Task<string?> RevisarAsync(ProyectoGanadero proyecto, CancellationToken ct = default)
    {
        var cadena = new FichaCompleta();
        cadena.Luego(new ProductorVerificado(db))
              .Luego(new DocumentosDelPredio(db));
        return cadena.RevisarAsync(proyecto, ct);
    }
}
