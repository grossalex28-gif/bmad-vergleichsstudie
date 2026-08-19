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
    public DbSet<BuchungsPosition> BuchungsPositionen => Set<BuchungsPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var listStringConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
            v => string.Join(',', v),
            v => v.Length == 0 ? new List<string>() : v.Split(',', StringSplitOptions.None).ToList());

        var listIntConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<int>, string>(
            v => string.Join(',', v),
            v => v.Length == 0 ? new List<int>() : v.Split(',', StringSplitOptions.None).Select(int.Parse).ToList());

        var listStringComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        var listIntComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<int>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<Spielstaette>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Raeume)
                .WithOne(e => e.Spielstaette)
                .HasForeignKey(e => e.SpielstaetteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Raum>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reihen)
                .HasConversion(listStringConverter)
                .Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.GangSpalten)
                .HasConversion(listIntConverter)
                .Metadata.SetValueComparer(listIntComparer);
        });

        modelBuilder.Entity<Veranstaltung>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titel).IsRequired();
            entity.HasOne(e => e.Spielstaette)
                .WithMany()
                .HasForeignKey(e => e.SpielstaetteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Raum)
                .WithMany(r => r.Veranstaltungen)
                .HasForeignKey(e => e.RaumId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Preiskategorien)
                .WithOne(p => p.Veranstaltung)
                .HasForeignKey(p => p.VeranstaltungId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Preiskategorie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Preis).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Buchung>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Referenz).IsUnique();
            entity.HasOne(e => e.Veranstaltung)
                .WithMany(v => v.Buchungen)
                .HasForeignKey(e => e.VeranstaltungId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<BuchungsPosition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Preis).HasPrecision(10, 2);
            entity.HasOne(e => e.Buchung)
                .WithMany(b => b.Positionen)
                .HasForeignKey(e => e.BuchungId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Preiskategorie)
                .WithMany()
                .HasForeignKey(e => e.PreiskategorieId)
                .OnDelete(DeleteBehavior.Restrict);

            // Verhindert Doppelbuchung desselben Sitzplatzes unter Nebenläufigkeit:
            // Die Datenbank lässt für einen aktiven (nicht stornierten) Sitzplatz
            // je Veranstaltung nur einen Eintrag zu.
            entity.HasIndex(e => new { e.VeranstaltungId, e.Reihe, e.Spalte })
                .IsUnique()
                .HasFilter("[Aktiv] = 1")
                .HasDatabaseName("IX_BuchungsPosition_Aktiv_Sitzplatz");
        });
    }
}
