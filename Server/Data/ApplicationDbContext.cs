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
    public DbSet<Ubicaciones> Ubicaciones { get; set; }
    public DbSet<Palets> Palets { get; set; }
    public DbSet<NSeriesRecepciones> NSeries_Recepciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Clave primaria para Referencias
        modelBuilder.Entity<Referencias>()
            .HasKey(r => r.Referencia);


        modelBuilder.Entity<NSeriesRecepciones>(entity =>
        {
            entity.HasKey(n => n.NSerie);
            entity.Property(n => n.NSerie)
                  .IsRequired()
                  .HasColumnName("NSERIE");
        });

        modelBuilder.Entity<NSeriesRecepciones>(entity =>
        {
            entity.HasKey(n => n.NSerie);

            entity.Property(n => n.NSerie)
                .IsRequired()
                .HasColumnName("NSERIE");

            // Relación con Referencias
            entity.HasOne<Referencias>()
                .WithMany()
                .HasForeignKey(n => n.Referencia)
                .HasPrincipalKey(r => r.Referencia);
        });

        modelBuilder.Entity<Palets>()
            .HasKey(p => p.Palet);

        modelBuilder.Entity<NSeriesRecepciones>()
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

        modelBuilder.Entity<Ubicaciones>()
        .HasKey(u => u.Ubicacion);

        modelBuilder.Entity<Palets>()
            .Property(p => p.Palet)
    .       ValueGeneratedOnAdd(); // Esto indica que es IDENTITY

        modelBuilder.Entity<NSeriesRecepciones>()
            .Property(n => n.NSerie)
            .HasColumnName("NSERIE");
    }
}