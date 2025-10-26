using Microsoft.EntityFrameworkCore;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecepcionesCab> Recepciones_Cab { get; set; }
    public DbSet<RecepcionesLin> Recepciones_Lin { get; set; }
    public DbSet<Referencias> Referencias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Clave primaria para Referencias
        modelBuilder.Entity<Referencias>()
            .HasKey(r => r.Referencia);

        // Clave primaria para RecepcionesCab
        modelBuilder.Entity<RecepcionesCab>()
            .HasKey(r => r.Albaran);

        // Clave primaria compuesta para líneas
        modelBuilder.Entity<RecepcionesLin>()
            .HasKey(rl => new { rl.Albaran, rl.Linea });

        // Relaciones FK
        modelBuilder.Entity<RecepcionesLin>()
            .HasOne<RecepcionesCab>()
            .WithMany()
            .HasForeignKey(rl => rl.Albaran);

        modelBuilder.Entity<RecepcionesLin>()
            .HasOne<Referencias>()
            .WithMany()
            .HasForeignKey(rl => rl.Referencia)
            .HasPrincipalKey(r => r.Referencia);
    }
}