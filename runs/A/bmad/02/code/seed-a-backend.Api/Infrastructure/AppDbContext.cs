using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Name des CHECK-Constraints, der erzwingt, dass `BookingPosition.BookingId`/`PriceCategoryId`
    /// immer gemeinsam null oder gemeinsam gesetzt sind (AD-2). Als gemeinsam referenzierte Konstante
    /// statt eines duplizierten String-Literals in Produktions- und Testcode, damit eine künftige
    /// Umbenennung bereits beim Kompilieren der Tests auffällt statt erst zur Laufzeit.
    /// </summary>
    internal static readonly string BookingPositionBeideNullOderBeideGesetztConstraintName =
        "CK_BookingPositions_BookingId_PriceCategoryId_BeideNullOderBeideGesetzt";

    /// <summary>Name des gefilterten Unique-Index, der doppelte Sitzplatzbelegungen verhindert (AD-1).</summary>
    internal static readonly string BookingPositionSeatUniqueConstraintName =
        "IX_BookingPositions_EventId_RowLabel_ColumnNumber";

    /// <summary>Name des Unique-Index auf der Buchungsreferenz (AD-4).</summary>
    internal static readonly string BookingReferenceUniqueConstraintName =
        "IX_Bookings_Reference";

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PriceCategory> PriceCategories => Set<PriceCategory>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingPosition> BookingPositions => Set<BookingPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasMany(v => v.Rooms)
                .WithOne()
                .HasForeignKey(r => r.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasMany(r => r.Events)
                .WithOne()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureJsonListConversion<IReadOnlyList<string>, string>(entity.Property(r => r.RowLabels), list => list);
            ConfigureJsonListConversion<IReadOnlyCollection<int>, int>(entity.Property(r => r.AisleColumns), list => list);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasMany(e => e.PriceCategories)
                .WithOne()
                .HasForeignKey(pc => pc.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.BookingPositions)
                .WithOne()
                .HasForeignKey(bp => bp.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceCategory>(entity =>
        {
            entity.Property(pc => pc.Preis).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.Reference)
                .HasMaxLength(8)
                .IsRequired()
                .UseCollation("SQL_Latin1_General_CP1_CI_AS");
            entity.HasIndex(b => b.Reference).IsUnique();

            entity.HasMany(b => b.Positions)
                .WithOne()
                .HasForeignKey(bp => bp.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingPosition>(entity =>
        {
            entity.HasOne<PriceCategory>()
                .WithMany()
                .HasForeignKey(bp => bp.PriceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(bp => new { bp.EventId, bp.RowLabel, bp.ColumnNumber })
                .IsUnique()
                .HasFilter("[CancelledAtUtc] IS NULL");

            entity.ToTable(t => t.HasCheckConstraint(
                BookingPositionBeideNullOderBeideGesetztConstraintName,
                "([BookingId] IS NULL AND [PriceCategoryId] IS NULL) OR ([BookingId] IS NOT NULL AND [PriceCategoryId] IS NOT NULL)"));
        });
    }

    /// <summary>
    /// Persistiert eine Listen-Property als JSON-String mit einem korrekten Value-Comparer für
    /// EF-Core-Change-Tracking. Gemeinsame Konfiguration für `Room.RowLabels`/`Room.AisleColumns`,
    /// die sich zuvor nur im Element-/Property-Typ unterschied. `fromList` wrappt das deserialisierte
    /// `List&lt;TItem&gt;` in `TCollection` beim Aufrufer statt per ungesichertem `(TCollection)(object)`-Cast,
    /// der nur zufällig funktioniert, wenn `List&lt;TItem&gt;` tatsächlich in `TCollection` konvertierbar ist.
    /// </summary>
    private static void ConfigureJsonListConversion<TCollection, TItem>(
        PropertyBuilder<TCollection> property,
        Func<List<TItem>, TCollection> fromList)
        where TCollection : class, IReadOnlyCollection<TItem>
    {
        var empty = fromList(new List<TItem>());

        property
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => fromList(JsonSerializer.Deserialize<List<TItem>>(v, JsonSerializerOptions.Default) ?? new List<TItem>()))
            .Metadata.SetValueComparer(new ValueComparer<TCollection>(
                (a, b) => (a ?? empty).SequenceEqual(b ?? empty),
                v => (v ?? empty).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                v => fromList((v ?? empty).ToList())));
    }
}
