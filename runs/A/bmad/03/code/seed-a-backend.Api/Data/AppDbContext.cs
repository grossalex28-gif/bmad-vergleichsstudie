using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Spielstaette> Spielstaetten => Set<Spielstaette>();

    public DbSet<Raum> Raeume => Set<Raum>();

    public DbSet<Veranstaltung> Veranstaltungen => Set<Veranstaltung>();

    public DbSet<Preiskategorie> Preiskategorien => Set<Preiskategorie>();

    public DbSet<Sitzplatzbelegung> Sitzplatzbelegungen => Set<Sitzplatzbelegung>();

    public DbSet<Buchung> Buchungen => Set<Buchung>();

    public DbSet<Buchungsposition> Buchungspositionen => Set<Buchungsposition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringArrayComparer = new ValueComparer<string[]>(
            (a, b) => a!.SequenceEqual(b!),
            a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
            a => a.ToArray());

        var intArrayComparer = new ValueComparer<int[]>(
            (a, b) => a!.SequenceEqual(b!),
            a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
            a => a.ToArray());

        modelBuilder.Entity<Spielstaette>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Raeume)
                .WithOne(e => e.Spielstaette)
                .HasForeignKey(e => e.SpielstaetteId);
        });

        modelBuilder.Entity<Raum>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Reihen)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<string[]>(v, JsonSerializerOptions.Default) ?? Array.Empty<string>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(stringArrayComparer);

            entity.Property(e => e.GangSpalten)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<int[]>(v, JsonSerializerOptions.Default) ?? Array.Empty<int>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(intArrayComparer);

            entity.HasMany(e => e.Veranstaltungen)
                .WithOne(e => e.Raum)
                .HasForeignKey(e => e.RaumId);
        });

        modelBuilder.Entity<Veranstaltung>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Zeitpunkt).IsRequired();
            entity.HasMany(e => e.Preiskategorien)
                .WithOne(e => e.Veranstaltung)
                .HasForeignKey(e => e.VeranstaltungId);
        });

        modelBuilder.Entity<Preiskategorie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Preis).HasColumnType("decimal(6,2)");
        });

        modelBuilder.Entity<Sitzplatzbelegung>(entity =>
        {
            entity.HasKey(e => new { e.VeranstaltungId, e.SitzplatzCode });
            entity.Property(e => e.BuchungId).HasMaxLength(450);
            entity.HasOne(e => e.Veranstaltung)
                .WithMany()
                .HasForeignKey(e => e.VeranstaltungId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Buchung>()
                .WithMany()
                .HasForeignKey(e => e.BuchungId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Buchung>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Referenz).IsUnique();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Gesamtpreis).HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.Veranstaltung)
                .WithMany()
                .HasForeignKey(e => e.VeranstaltungId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Buchungsposition>(entity =>
        {
            entity.HasKey(e => new { e.BuchungId, e.SitzplatzCode });
            entity.Property(e => e.PreisSnapshot).HasColumnType("decimal(6,2)");
            entity.HasOne(e => e.Buchung)
                .WithMany(e => e.Buchungspositionen)
                .HasForeignKey(e => e.BuchungId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Preiskategorie)
                .WithMany()
                .HasForeignKey(e => e.PreiskategorieId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
