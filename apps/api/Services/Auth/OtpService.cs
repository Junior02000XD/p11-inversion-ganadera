using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using P11.Api.Configuration;
using P11.Api.Data;
using P11.Api.Models;
using P11.Api.Services.Email;

namespace P11.Api.Services.Auth;

public enum ResultadoOtp { Valido, Incorrecto, Vencido, SinIntentos, Inexistente }

/// <summary>Genera y valida códigos OTP de un solo uso (RNF-01). Solo se guarda el hash.</summary>
public class OtpService(AppDbContext db, ICorreoService correo, IOptions<OtpOptions> opciones, TimeProvider reloj)
{
    private readonly OtpOptions _o = opciones.Value;

    public async Task GenerarYEnviarAsync(Usuario usuario, PropositoOtp proposito, CancellationToken ct = default)
    {
        // Un código nuevo anula los anteriores del mismo propósito.
        var previos = await db.CodigosOtp
            .Where(c => c.UsuarioId == usuario.Id && c.Proposito == proposito && c.UsadoEn == null && !c.Anulado)
            .ToListAsync(ct);
        previos.ForEach(c => c.Anulado = true);

        var codigo = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, _o.Digitos)).ToString($"D{_o.Digitos}");
        db.CodigosOtp.Add(new CodigoOtp
        {
            UsuarioId = usuario.Id,
            Proposito = proposito,
            CodigoHash = Hash(usuario.Id, codigo),
            ExpiraEn = reloj.GetUtcNow().AddMinutes(_o.MinutosVigencia),
        });
        await db.SaveChangesAsync(ct);

        var para = proposito == PropositoOtp.FirmarContrato ? "firmar el contrato de inversión" : "confirmar la cuenta";
        await correo.EnviarAsync(usuario.Email!, $"Tu código de verificación: {codigo}",
            $"Tu código para {para} es {codigo}.\nVence en {_o.MinutosVigencia} minutos. " +
            "Si no fuiste tú, ignora este correo.", ct);
    }

    public async Task<ResultadoOtp> ValidarAsync(Guid usuarioId, PropositoOtp proposito, string codigo, CancellationToken ct = default)
    {
        var otp = await db.CodigosOtp
            .Where(c => c.UsuarioId == usuarioId && c.Proposito == proposito && c.UsadoEn == null && !c.Anulado)
            .OrderByDescending(c => c.ExpiraEn)
            .FirstOrDefaultAsync(ct);
        if (otp is null) return ResultadoOtp.Inexistente;
        if (reloj.GetUtcNow() > otp.ExpiraEn) return ResultadoOtp.Vencido;
        if (otp.Intentos >= _o.MaxIntentos) return ResultadoOtp.SinIntentos;

        var esperado = Convert.FromHexString(otp.CodigoHash);
        var recibido = Convert.FromHexString(Hash(usuarioId, (codigo ?? "").Trim()));
        if (!CryptographicOperations.FixedTimeEquals(esperado, recibido))
        {
            otp.Intentos++;
            await db.SaveChangesAsync(ct);
            return otp.Intentos >= _o.MaxIntentos ? ResultadoOtp.SinIntentos : ResultadoOtp.Incorrecto;
        }

        otp.UsadoEn = reloj.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return ResultadoOtp.Valido;
    }

    private static string Hash(Guid usuarioId, string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{usuarioId:N}:{codigo}")));
}
