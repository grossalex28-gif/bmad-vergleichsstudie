using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

public class SeedDataImporterTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    /// <summary>
    /// Dedizierte Test-Fixture, unabhängig vom Produktions-Anfangsdatenbestand — inhaltliche
    /// Änderungen an der Produktions-Seed-Datei dürfen diese Tests nicht brechen.
    /// </summary>
    private static string SeedFilePath() => Path.Combine(AppContext.BaseDirectory, "TestData", "seed-fixture.json");

    [Fact]
    public async Task ImportIfEmptyAsync_gegen_leere_DB_befuellt_alle_Tabellen_korrekt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);

        await importer.ImportIfEmptyAsync(SeedFilePath(), ct);

        Assert.Equal(2, await db.Venues.CountAsync(ct));
        Assert.Equal(3, await db.Rooms.CountAsync(ct));
        Assert.Equal(2, await db.Events.CountAsync(ct));
        Assert.Equal(4, await db.PriceCategories.CountAsync(ct));

        var kleinerSaal = await db.Rooms.SingleAsync(r => r.Name == "Kleiner Saal", ct);
        Assert.Equal(new[] { "A", "B", "C", "D", "E", "F" }, kleinerSaal.RowLabels);
        Assert.Equal(10, kleinerSaal.ColumnCount);
        Assert.Equal(new[] { 6 }, kleinerSaal.AisleColumns);

        var studio = await db.Rooms.SingleAsync(r => r.Name == "Studio", ct);
        Assert.Empty(studio.AisleColumns);

        var kammerkonzert = await db.Events.SingleAsync(e => e.Titel == "Kammerkonzert Fruehling", ct);

        var preiskategorien = await db.PriceCategories
            .Where(pc => pc.EventId == kammerkonzert.Id)
            .OrderBy(pc => pc.Name)
            .ToListAsync(ct);
        Assert.Collection(preiskategorien,
            pc => { Assert.Equal("Kategorie A", pc.Name); Assert.Equal(32.00m, pc.Preis); },
            pc => { Assert.Equal("Kategorie B", pc.Name); Assert.Equal(22.00m, pc.Preis); });

        var belegtePositionen = await db.BookingPositions
            .Where(bp => bp.EventId == kammerkonzert.Id)
            .OrderBy(bp => bp.RowLabel).ThenBy(bp => bp.ColumnNumber)
            .ToListAsync(ct);

        Assert.Equal(3, belegtePositionen.Count);
        Assert.Collection(belegtePositionen,
            p => Assert.Equal(("B", 3), (p.RowLabel, p.ColumnNumber)),
            p => Assert.Equal(("B", 4), (p.RowLabel, p.ColumnNumber)),
            p => Assert.Equal(("C", 7), (p.RowLabel, p.ColumnNumber)));

        Assert.All(belegtePositionen, p =>
        {
            Assert.Null(p.BookingId);
            Assert.Null(p.PriceCategoryId);
            Assert.Null(p.CancelledAtUtc);
        });

        var sinfonisches = await db.Events.SingleAsync(e => e.Titel == "Sinfonisches Openair-Programm", ct);
        var keinePositionen = await db.BookingPositions.Where(bp => bp.EventId == sinfonisches.Id).CountAsync(ct);
        Assert.Equal(0, keinePositionen);
    }

    [Fact]
    public async Task ImportIfEmptyAsync_zweiter_Lauf_gegen_befuellte_DB_erzeugt_keine_Duplikate()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        var seedFilePath = SeedFilePath();

        await importer.ImportIfEmptyAsync(seedFilePath, ct);
        var venuesNachErstemLauf = await db.Venues.CountAsync(ct);
        var eventsNachErstemLauf = await db.Events.CountAsync(ct);
        var positionenNachErstemLauf = await db.BookingPositions.CountAsync(ct);

        await importer.ImportIfEmptyAsync(seedFilePath, ct);

        Assert.Equal(venuesNachErstemLauf, await db.Venues.CountAsync(ct));
        Assert.Equal(eventsNachErstemLauf, await db.Events.CountAsync(ct));
        Assert.Equal(positionenNachErstemLauf, await db.BookingPositions.CountAsync(ct));
    }

    [Fact]
    public async Task Gefilterte_Unique_Constraint_verhindert_doppelte_aktive_Belegung_desselben_Sitzplatzes()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        await importer.ImportIfEmptyAsync(SeedFilePath(), ct);

        var kammerkonzert = await db.Events.SingleAsync(e => e.Titel == "Kammerkonzert Fruehling", ct);

        db.BookingPositions.Add(new Api.Domain.BookingPosition
        {
            EventId = kammerkonzert.Id,
            RowLabel = "B",
            ColumnNumber = 3,
            BookingId = null,
            PriceCategoryId = null,
            CancelledAtUtc = null,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Nach_Stornierung_kann_derselbe_Sitzplatz_erneut_belegt_werden()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        await importer.ImportIfEmptyAsync(SeedFilePath(), ct);

        var kammerkonzert = await db.Events.SingleAsync(e => e.Titel == "Kammerkonzert Fruehling", ct);
        var stornierterPlatz = await db.BookingPositions.SingleAsync(
            bp => bp.EventId == kammerkonzert.Id && bp.RowLabel == "B" && bp.ColumnNumber == 3, ct);
        stornierterPlatz.CancelledAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        db.BookingPositions.Add(new Api.Domain.BookingPosition
        {
            EventId = kammerkonzert.Id,
            RowLabel = "B",
            ColumnNumber = 3,
            BookingId = null,
            PriceCategoryId = null,
            CancelledAtUtc = null,
        });

        await db.SaveChangesAsync(ct);

        var aktiveBelegungen = await db.BookingPositions.CountAsync(
            bp => bp.EventId == kammerkonzert.Id && bp.RowLabel == "B" && bp.ColumnNumber == 3 && bp.CancelledAtUtc == null, ct);
        Assert.Equal(1, aktiveBelegungen);
    }

    [Fact]
    public async Task PriceCategory_Preis_wird_gemaess_HasPrecision_10_2_auf_zwei_Nachkommastellen_gerundet()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        await importer.ImportIfEmptyAsync(SeedFilePath(), ct);
        var kammerkonzert = await db.Events.SingleAsync(e => e.Titel == "Kammerkonzert Fruehling", ct);

        db.PriceCategories.Add(new Api.Domain.PriceCategory
        {
            EventId = kammerkonzert.Id,
            Name = "Praezisionstest",
            Preis = 19.999m,
        });
        await db.SaveChangesAsync(ct);

        await using var frischeVerbindung = _database.CreateContext();
        var gespeichert = await frischeVerbindung.PriceCategories.SingleAsync(pc => pc.Name == "Praezisionstest", ct);
        Assert.Equal(20.00m, gespeichert.Preis);
    }

    [Fact]
    public async Task Raum_mit_beidseitigem_Gang_wird_1_zu_1_aus_dem_Seed_uebernommen()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var importer = new SeedDataImporter(db);
        await importer.ImportIfEmptyAsync(SeedFilePath(), ct);

        var konzertsaal = await db.Rooms.SingleAsync(r => r.Name == "Konzertsaal", ct);

        Assert.Equal(new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" }, konzertsaal.RowLabels);
        Assert.Equal(16, konzertsaal.ColumnCount);
        Assert.Equal(new[] { 1, 16 }, konzertsaal.AisleColumns);
    }
}
