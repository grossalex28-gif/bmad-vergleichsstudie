using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Spielstaette> Spielstaetten => Set<Spielstaette>();
    public DbSet<Raum> Raeume => Set<Raum>();
    public DbSet<Veranstaltung> Veranstaltungen => Set<Veranstaltung>();
    public DbSet<Preiskategorie> Preiskategorien => Set<Preiskategorie>();
    public DbSet<Buchung> Buchungen => Set<Buchung>();
    public DbSet<Buchungsposition> Buchungspositionen => Set<Buchungsposition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Spielstaette>(entity =>
        {
            entity.HasMany(s => s.Raeume)
                .WithOne(r => r.Spielstaette)
                .HasForeignKey(r => r.SpielstaetteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Raum>(entity =>
        {
            entity.HasMany(r => r.Veranstaltungen)
                .WithOne(v => v.Raum)
                .HasForeignKey(v => v.RaumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Veranstaltung>(entity =>
        {
            entity.HasOne(v => v.Spielstaette)
                .WithMany()
                .HasForeignKey(v => v.SpielstaetteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.Preiskategorien)
                .WithOne(p => p.Veranstaltung)
                .HasForeignKey(p => p.VeranstaltungId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Preiskategorie>(entity =>
        {
            entity.Property(p => p.Preis).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Buchung>(entity =>
        {
            entity.HasIndex(b => b.Referenz).IsUnique().HasDatabaseName("IX_Buchung_Referenz");

            entity.HasOne(b => b.Veranstaltung)
                .WithMany()
                .HasForeignKey(b => b.VeranstaltungId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.Positionen)
                .WithOne(p => p.Buchung)
                .HasForeignKey(p => p.BuchungId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Buchungsposition>(entity =>
        {
            entity.Property(p => p.Preis).HasColumnType("decimal(10,2)");

            entity.HasOne(p => p.Veranstaltung)
                .WithMany(v => v.Buchungspositionen)
                .HasForeignKey(p => p.VeranstaltungId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Preiskategorie)
                .WithMany()
                .HasForeignKey(p => p.PreiskategorieId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ein Sitzplatz darf pro Veranstaltung nur einmal aktiv belegt sein.
            // Der DB-Unique-Index ist die verbindliche Instanz gegen Wettlaufbedingungen
            // bei gleichzeitigen Buchungsversuchen auf denselben Sitzplatz (A-F13).
            entity.HasIndex(p => new { p.VeranstaltungId, p.Reihe, p.Spalte })
                .IsUnique()
                .HasFilter("[Status] = 0")
                .HasDatabaseName("IX_Buchungsposition_AktiverSitzplatz");
        });
    }
}
