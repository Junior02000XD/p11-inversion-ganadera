using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dtos;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Models.Investments;
using P11.Api.Models.Projects;
using P11.Api.Services.Investments;
using P11.Api.Services.Auth;

namespace P11.Api.Controllers;

/// <summary>
/// CU «Invertir en un proyecto», paso a paso:
/// 1) reservar el cupo (RF-13), 2) aceptar el contrato con un código OTP (RF-14),
/// 3) pagar con QR (RF-15) y 4) recibir la participación con su constancia (RF-16).
/// Cada inversionista ve y maneja solo sus inversiones.
/// </summary>
[ApiController]
[Route("api/inversiones")]
[Authorize(Roles = $"{Roles.Inversionista},{Roles.Administrador}")]
[Produces("application/json")]
public class InversionesController(
    AppDbContext db, GestorInversiones gestor, OtpService otp, UserManager<Usuario> usuarios) : ControllerBase
{
    /// <summary>Mis inversiones, de la más nueva a la más vieja (todas: en curso, pagadas, vencidas y rechazadas).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InversionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InversionDto>>> Listar(CancellationToken ct)
    {
        var mias = await Mias().OrderByDescending(i => i.CreadaEn).ToListAsync(ct);
        foreach (var inversion in mias) await gestor.VencerSiCorrespondeAsync(inversion, ct);
        return await ADtosAsync(mias, ct);
    }

    /// <summary>Una de mis inversiones; 404 si no existe o es de otra persona.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<InversionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InversionDto>> Obtener(Guid id, CancellationToken ct)
    {
        var (inversion, proyecto) = await BuscarAsync(id, ct);
        if (inversion is null || proyecto is null) return NoEncontrada();
        await gestor.VencerSiCorrespondeAsync(inversion, ct);
        return InversionDto.Desde(inversion, proyecto);
    }

    /// <summary>
    /// RF-13: reserva el cupo por 30 minutos. 400 si el monto está bajo el mínimo; 409 si supera el cupo libre,
    /// si la identidad no está verificada (RF-04) o si ya hay una reserva en curso en ese proyecto.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<InversionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InversionDto>> Reservar(ReservaRequest req, CancellationToken ct)
    {
        var usuario = await usuarios.FindByIdAsync(User.IdUsuario().ToString());
        if (usuario?.EstadoVerificacion != EstadoVerificacion.Verificado)
            return Conflict(new Respuesta("Tu identidad todavía no está verificada: no puedes invertir hasta que lo esté."));

        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == req.ProyectoId && p.Estado == EstadoProyecto.EnFinanciamiento, ct);
        if (proyecto is null) return NotFound(new Respuesta("Ese proyecto no está en el catálogo."));
        if (gestor.Ahora >= proyecto.CierreRondaEn) return Conflict(new Respuesta("La ronda de este proyecto ya cerró."));

        var cupo = await gestor.CupoAsync(proyecto.Id, proyecto.MontoRequeridoBs, ct);
        if (req.MontoBs < proyecto.MontoMinimoBs)
        {
            ModelState.AddModelError(nameof(req.MontoBs),
                $"El mínimo para invertir es {FormatoBolivia.Bs(proyecto.MontoMinimoBs)}. Cupo disponible: {FormatoBolivia.Bs(cupo.LibreBs)}.");
            return ValidationProblem(ModelState);
        }
        if (req.MontoBs > cupo.LibreBs)
            return Conflict(new Respuesta($"El monto supera el cupo disponible: quedan {FormatoBolivia.Bs(cupo.LibreBs)}."));

        var enCurso = await Mias().Where(i => i.ProyectoId == proyecto.Id
            && (i.Estado == EstadoInversion.Reservada || i.Estado == EstadoInversion.Firmada)).ToListAsync(ct);
        if (enCurso.Any(i => !i.Vencio(gestor.Ahora)))
            return Conflict(new Respuesta("Ya tienes una reserva en curso en este proyecto: termínala o espera a que venza."));

        // Si dos personas reservan a la vez puede pasarse del cupo por unos bolivianos; con pocos usuarios se acepta.
        var inversion = Inversion.Reservar(proyecto.Id, usuario.Id, req.MontoBs, gestor.Ahora);
        db.Inversiones.Add(inversion);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = inversion.Id }, InversionDto.Desde(inversion, proyecto));
    }

    /// <summary>RF-14: el contrato y los riesgos que se aceptan, con su versión.</summary>
    [HttpGet("{id:guid}/contrato")]
    [ProducesResponseType<ContratoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContratoDto>> Contrato(Guid id, CancellationToken ct)
    {
        var (inversion, proyecto) = await BuscarAsync(id, ct);
        if (inversion is null || proyecto is null) return NoEncontrada();
        return new ContratoDto(inversion.Id, ContratoInversion.Version, ContratoInversion.Texto(proyecto, inversion.MontoBs),
            proyecto.Riesgos, inversion.MontoBs, inversion.ReservadaHasta);
    }

    /// <summary>RF-14: envía al correo el código OTP para firmar el contrato.</summary>
    [HttpPost("{id:guid}/contrato/codigo")]
    [ProducesResponseType<Respuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respuesta>> EnviarCodigo(Guid id, CancellationToken ct)
    {
        var (inversion, _) = await BuscarAsync(id, ct);
        if (inversion is null) return NoEncontrada();
        if (await gestor.VencerSiCorrespondeAsync(inversion, ct)) return Conflict(new Respuesta(ReservaVencidaException.Mensaje));
        if (inversion.Estado != EstadoInversion.Reservada) return Conflict(new Respuesta("El contrato de esta inversión ya está firmado."));

        var usuario = await usuarios.FindByIdAsync(User.IdUsuario().ToString());
        await otp.GenerarYEnviarAsync(usuario!, PropositoOtp.FirmarContrato, ct);
        return new Respuesta("Te enviamos un código a tu correo para firmar el contrato.");
    }

    /// <summary>
    /// RF-14: acepta el contrato y los riesgos con el código OTP. Guarda quién, cuándo y qué versión firmó;
    /// esa evidencia no se puede borrar. 400 si no aceptó los riesgos o el código no es válido.
    /// </summary>
    [HttpPost("{id:guid}/contrato/firmar")]
    [ProducesResponseType<InversionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InversionDto>> Firmar(Guid id, FirmaContratoRequest req, CancellationToken ct)
    {
        var (inversion, proyecto) = await BuscarAsync(id, ct);
        if (inversion is null || proyecto is null) return NoEncontrada();
        if (!req.AceptaRiesgos) return BadRequest(new Respuesta("Debes aceptar el contrato y los riesgos para continuar."));
        if (await gestor.VencerSiCorrespondeAsync(inversion, ct)) return Conflict(new Respuesta(ReservaVencidaException.Mensaje));

        var resultado = await otp.ValidarAsync(User.IdUsuario(), PropositoOtp.FirmarContrato, req.Codigo, ct);
        if (resultado != ResultadoOtp.Valido) return BadRequest(new Respuesta(OtpService.Mensaje(resultado)));

        var error = await AplicarAsync(inversion, i => i.FirmarContrato(ContratoInversion.Version, User.Correo(), gestor.Ahora), ct);
        if (error is not null) return Conflict(error);
        return InversionDto.Desde(inversion, proyecto);
    }

    /// <summary>RF-15: genera el QR de pago simulado. Sin contrato firmado no se habilita (409).</summary>
    [HttpPost("{id:guid}/pago")]
    [ProducesResponseType<PagoQrDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagoQrDto>> GenerarPago(Guid id, CancellationToken ct)
    {
        var (inversion, proyecto) = await BuscarAsync(id, ct);
        if (inversion is null || proyecto is null) return NoEncontrada();

        var error = await AplicarAsync(inversion, i => i.PrepararPago(gestor.Ahora), ct);
        if (error is not null) return Conflict(error);

        // Texto del QR: P11|referencia|moneda|monto|proyecto. Lo lee la pasarela (hoy simulada).
        var contenido = $"P11|{inversion.ReferenciaPago}|BOB|{inversion.MontoBs:0.00}|{proyecto.Nombre}";
        return new PagoQrDto(inversion.ReferenciaPago!, inversion.MontoBs, contenido, inversion.ReservadaHasta);
    }

    /// <summary>
    /// RF-15 y RF-16: simula el aviso de la pasarela. Aprobado: queda pagada, con su participación y constancia.
    /// Rechazado o reserva vencida: el cupo se libera y se avisa al inversionista.
    /// </summary>
    [HttpPost("{id:guid}/pago/confirmar")]
    [ProducesResponseType<InversionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Respuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InversionDto>> ConfirmarPago(Guid id, ConfirmacionPagoRequest req, CancellationToken ct)
    {
        var (inversion, proyecto) = await BuscarAsync(id, ct);
        if (inversion is null || proyecto is null) return NoEncontrada();

        if (!req.Aprobado)
        {
            var errorRechazo = await AplicarAsync(inversion, i => i.RechazarPago(), ct);
            if (errorRechazo is not null) return Conflict(errorRechazo);
            await gestor.AvisarLiberacionAsync(inversion, "la pasarela rechazó el pago.", ct);
            return InversionDto.Desde(inversion, proyecto);
        }

        var errorPago = await AplicarAsync(inversion, i => i.ConfirmarPago(proyecto, gestor.Ahora), ct);
        if (errorPago is not null) return Conflict(errorPago);
        await gestor.AvisarParticipacionAsync(inversion, ct);
        return InversionDto.Desde(inversion, proyecto);
    }

    /// <summary>
    /// Aplica una regla de la inversión y guarda. Devuelve null si salió bien, o el motivo si la regla no
    /// se cumple (y si la reserva venció, antes la marca vencida y avisa).
    /// </summary>
    private async Task<Respuesta?> AplicarAsync(Inversion inversion, Action<Inversion> regla, CancellationToken ct)
    {
        try
        {
            regla(inversion);
        }
        catch (ReservaVencidaException ex)
        {
            await gestor.VencerSiCorrespondeAsync(inversion, ct);
            return new Respuesta(ex.Message);
        }
        catch (ReglaInversionException ex)
        {
            return new Respuesta(ex.Message);
        }
        await db.SaveChangesAsync(ct);
        return null;
    }

    private IQueryable<Inversion> Mias()
    {
        var yo = User.IdUsuario();
        return db.Inversiones.Where(i => i.InversionistaId == yo);
    }

    private async Task<(Inversion?, ProyectoGanadero?)> BuscarAsync(Guid id, CancellationToken ct)
    {
        var inversion = await Mias().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inversion is null) return (null, null);
        return (inversion, await db.Proyectos.FindAsync([inversion.ProyectoId], ct));
    }

    private async Task<List<InversionDto>> ADtosAsync(IReadOnlyList<Inversion> lista, CancellationToken ct)
    {
        var ids = lista.Select(i => i.ProyectoId).Distinct().ToList();
        var proyectos = await db.Proyectos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        return lista.Select(i => InversionDto.Desde(i, proyectos[i.ProyectoId])).ToList();
    }

    private NotFoundObjectResult NoEncontrada() => NotFound(new Respuesta("No encontramos esa inversión."));
}
