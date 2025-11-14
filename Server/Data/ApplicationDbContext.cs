using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Models;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecepcionesCab> Recepciones_Cab { get; set; }
    public DbSet<OrdenSalidaCab> Orden_Salida_Cab { get; set; }
    public DbSet<RecepcionesLin> Recepciones_Lin { get; set; }
    public DbSet<OrdenSalidaLin> Orden_Salida_Lin { get; set; }
    public DbSet<Referencias> Referencias { get; set; }
    public DbSet<Ubicaciones> Ubicaciones { get; set; }
    public DbSet<Palets> Palets { get; set; }
    public DbSet<NSeriesRecepciones> NSeries_Recepciones { get; set; }
    public DbSet<NSeriesSeguimiento> NSeries_Seguimiento { get; set; }
    public DbSet<VistaOrdenSalidaCab> V_OSC_ESTADO_DESCRIPCION { get; set; }
    public DbSet<VistaPaletsReservados> V_MOVIMIENTO_PALETS { get; set; }
    public DbSet<Usuarios> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<RecepcionesCab>()
            .HasKey(r => r.Albaran);

        // RecepcionesLin (clave compuesta)
        modelBuilder.Entity<RecepcionesLin>()
            .HasKey(rl => new { rl.Albaran, rl.Linea });

        modelBuilder.Entity<Referencias>()
            .HasKey(r => r.Referencia);

        modelBuilder.Entity<Usuarios>()
            .HasKey(u => u.IdUsuario);

        modelBuilder.Entity<Palets>()
            .HasKey(p => p.Palet);

        modelBuilder.Entity<VistaOrdenSalidaCab>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("V_OSC_ESTADO_DESCRIPCION");
        });
        modelBuilder.Entity<VistaPaletsReservados>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("V_MOVIMIENTO_PALETS");
        });

        modelBuilder.Entity<NSeriesSeguimiento>()
            .HasKey(nss => nss.NSerie);

        modelBuilder.Entity<NSeriesRecepciones>()
            .HasKey(ns => ns.NSerie);

        modelBuilder.Entity<OrdenSalidaCab>()
            .HasKey(osc => osc.Peticion);

        modelBuilder.Entity<OrdenSalidaLin>()
            .HasKey(osl => new { osl.Peticion, osl.Linea });

        modelBuilder.Entity<OrdenSalidaLin>()
            .HasOne<OrdenSalidaCab>()
            .WithMany()
            .HasForeignKey(osc => osc.Peticion);

        modelBuilder.Entity<RecepcionesLin>()
            .HasOne<RecepcionesCab>()
            .WithMany()
            .HasForeignKey(rl => rl.Albaran);

        modelBuilder.Entity<RecepcionesLin>()
            .HasOne<Referencias>()
            .WithMany()
            .HasForeignKey(rl => rl.Referencia)
            .HasPrincipalKey(r => r.Referencia);

        modelBuilder.Entity<Palets>()
            .HasOne<RecepcionesCab>()
            .WithMany()
            .HasForeignKey(p => p.Albaran);

        modelBuilder.Entity<Palets>()
            .HasOne<Referencias>()
            .WithMany()
            .HasForeignKey(p => p.Referencia)
            .HasPrincipalKey(r => r.Referencia);

        
        modelBuilder.Entity<NSeriesRecepciones>()
            .HasOne<Palets>()
            .WithMany()
            .HasForeignKey(ns => ns.Palet)
            .HasPrincipalKey(p => p.Palet);

        modelBuilder.Entity<Ubicaciones>()
            .HasKey(u => u.Ubicacion);
    }
}