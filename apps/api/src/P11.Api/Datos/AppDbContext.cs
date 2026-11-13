using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using P11.Api.Dominio;
using P11.Api.Dominio.Proyectos;

namespace P11.Api.Datos;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CodigoOtp> CodigosOtp => Set<CodigoOtp>();
    public DbSet<HistorialVerificacion> HistorialVerificaciones => Set<HistorialVerificacion>();
    public DbSet<DocumentoArchivo> Documentos => Set<DocumentoArchivo>();
    public DbSet<ProyectoGanadero> Proyectos => Set<ProyectoGanadero>();
    public DbSet<HistorialProyecto> HistorialProyectos => Set<HistorialProyecto>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Usuario>(e =>
        {
            e.Property(u => u.Ci).HasMaxLength(20).IsRequired();
            e.Property(u => u.NombreCompleto).HasMaxLength(120).IsRequired();
            e.HasIndex(u => u.Ci).IsUnique();
            e.Property(u => u.EstadoVerificacion).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<CodigoOtp>(e =>
        {
            e.Property(o => o.Proposito).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(o => new { o.UsuarioId, o.Proposito });
            e.HasOne<Usuario>().WithMany().HasForeignKey(o => o.UsuarioId);
        });

        b.Entity<HistorialVerificacion>(e =>
        {
            e.Property(h => h.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(h => h.UsuarioId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId);
        });

        b.Entity<DocumentoArchivo>(e =>
        {
            e.Property(d => d.Tipo).HasConversion<string>().HasMaxLength(30);
            e.Property(d => d.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(d => d.UsuarioId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId);
        });

        // Una tabla para las dos líneas (TPH); el discriminador es la línea productiva.
        b.Entity<ProyectoGanadero>(e =>
        {
            e.ToTable("Proyectos");
            e.Ignore(p => p.Linea);
            e.Ignore(p => p.EstadoActual);
            e.Property(p => p.Observaciones).HasMaxLength(1000);
            e.HasDiscriminator<string>("LineaProductiva")
                .HasValue<ProyectoEngorde>(nameof(LineaProductiva.Engorde))
                .HasValue<ProyectoLecheria>(nameof(LineaProductiva.Lecheria));
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Nombre).HasMaxLength(150);
            e.Property(p => p.Ubicacion).HasMaxLength(200);
            e.HasIndex(p => p.ProductorId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(p => p.ProductorId);
        });

        b.Entity<HistorialProyecto>(e =>
        {
            e.Property(h => h.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.Accion).HasMaxLength(40);
            e.Property(h => h.Responsable).HasMaxLength(256);
            e.Property(h => h.Motivo).HasMaxLength(1000);
            e.HasIndex(h => h.ProyectoId);
            // La bitácora no se borra en cascada con el proyecto (RNF-03).
            e.HasOne<ProyectoGanadero>().WithMany().HasForeignKey(h => h.ProyectoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
