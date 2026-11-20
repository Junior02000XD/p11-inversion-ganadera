using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Contratos;
using P11.Api.Datos;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;
using P11.Api.Dominio.Simulacion;
using P11.Api.Servicios.Inversiones;
using P11.Api.Servicios.Simulacion;

namespace P11.Api.Controllers;

/// <summary>
/// CU «Explorar catálogo y simular escenarios»: RF-11 (catálogo con filtros) y RF-12 (simulador).
/// Solo muestra proyectos publicados («En financiamiento»).
/// </summary>
[ApiController]
[Route("api/catalogo")]
[Authorize(Roles = $"{Roles.Inversionista},{Roles.Administrador}")]
[Produces("application/json")]
public class CatalogoController(AppDbContext db, GestorInversiones inversiones) : ControllerBase
{
    /// <summary>RF-11: proyectos publicados. Cada filtro es opcional; los que se envían se combinan.</summary>
    /// <param name="linea">Engorde o Lecheria.</param>
    /// <param name="plazoMaximo">Plazo del proyecto, en meses, como máximo.</param>
    /// <param name="minimoHasta">Solo proyectos cuya inversión mínima es menor o igual a este monto (Bs).</param>
    /// <param name="zona">Texto que debe aparecer en la ubicación (por ejemplo «Warnes»).</param>
    /// <param name="riesgo">Bajo, Medio o Alto.</param>
    /// <param name="ct">Cancelación de la petición.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProyectoCatalogoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProyectoCatalogoDto>>> Listar(
        [FromQuery] LineaProductiva? linea, [FromQuery] int? plazoMaximo, [FromQuery] decimal? minimoHasta,
        [FromQuery] string? zona, [FromQuery] NivelRiesgo? riesgo, CancellationToken ct)
    {
        var consulta = Publicados();
        if (linea == LineaProductiva.Engorde) consulta = consulta.Where(p => p is ProyectoEngorde);
        if (linea == LineaProductiva.Lecheria) consulta = consulta.Where(p => p is ProyectoLecheria);
        if (plazoMaximo is { } plazo) consulta = consulta.Where(p => p.PlazoMeses <= plazo);
        if (minimoHasta is { } tope) consulta = consulta.Where(p => p.MontoMinimoBs <= tope);
        if (riesgo is { } nivel) consulta = consulta.Where(p => p.NivelRiesgo == nivel);
        if (!string.IsNullOrWhiteSpace(zona))
        {
            var texto = zona.Trim().ToLower();
            consulta = consulta.Where(p => p.Ubicacion.ToLower().Contains(texto));
        }

        var proyectos = await consulta.OrderBy(p => p.CierreRondaEn).ToListAsync(ct);
        var cupos = await inversiones.CuposAsync(proyectos.Select(p => (p.Id, p.MontoRequeridoBs)).ToList(), ct);
        return proyectos.Select(p => ProyectoCatalogoDto.Desde(p, cupos[p.Id])).ToList();
    }

    /// <summary>Un proyecto del catálogo, con sus riesgos y supuestos.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProyectoCatalogoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProyectoCatalogoDto>> Obtener(Guid id, CancellationToken ct)
    {
        var proyecto = await Publicados().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto is null) return NotFound(new Respuesta("Ese proyecto no está en el catálogo."));
        return ProyectoCatalogoDto.Desde(proyecto, await inversiones.CupoAsync(proyecto.Id, proyecto.MontoRequeridoBs, ct));
    }

    /// <summary>RF-12: tres escenarios para el monto indicado, con sus supuestos y la advertencia de RNF-06.</summary>
    [HttpPost("{id:guid}/simulacion")]
    [ProducesResponseType<SimulacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SimulacionDto>> Simular(Guid id, SimulacionRequest req, CancellationToken ct)
    {
        var proyecto = await Publicados().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto is null) return NotFound(new Respuesta("Ese proyecto no está en el catálogo."));

        var escenarios = SimuladorEscenarios.Simular(proyecto, req.MontoBs)
            .Select(e => new EscenarioDto(e.Tipo, e.RendimientoPct, e.CostosPct, e.MortalidadPct, e.ValorEstimadoBs, e.ResultadoBs(req.MontoBs)))
            .ToList();
        return new SimulacionDto(proyecto.Id, req.MontoBs, proyecto.MontoMinimoBs, escenarios, ConfiguracionEscenario.Advertencia, proyecto.Riesgos);
    }

    private IQueryable<ProyectoGanadero> Publicados() => db.Proyectos.Where(p => p.Estado == EstadoProyecto.EnFinanciamiento);
}
