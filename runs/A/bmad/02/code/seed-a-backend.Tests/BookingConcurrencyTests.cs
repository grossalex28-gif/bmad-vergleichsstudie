using System.Linq;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

/// <summary>
/// Beweist, dass die bestehende Konflikterkennung in <see cref="BookingService"/> (AD-1) auch unter
/// echter Nebenläufigkeit gegen den echten SQL Server korrekt entscheidet: genau ein Versuch gewinnt
/// bei überlappenden Sitzplatzmengen (NFR-1), disjunkte Sitzplatzmengen blockieren sich nicht (NFR-2).
/// Läuft bewusst gegen <see cref="SqlServerTestDatabase"/>, nicht gegen den EF-Core-InMemory-Provider,
/// da nur der echte gefilterte Unique-Index reale Constraint-Verletzungen unter Nebenläufigkeit erzeugt.
/// </summary>
public class BookingConcurrencyTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private async Task<(int EventId, int PriceCategoryId)> SeedEventAsync(CancellationToken ct)
    {
        await using var db = _database.CreateContext();
        var @event = new Event
        {
            Titel = "Testkonzert", Beschreibung = "Beschreibung",
            Zeitpunkt = DateTime.UtcNow, DauerMinuten = 90, Altersfreigabe = 0
        };
        var room = new Room { Name = "Saal 1", RowLabels = ["A"], ColumnCount = 10, Events = [@event] };
        var venue = new Venue { Name = "Testhalle", Rooms = [room] };
        var priceCategory = new PriceCategory { Name = "Standard", Preis = 10m };
        @event.PriceCategories.Add(priceCategory);

        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return (@event.Id, priceCategory.Id);
    }

    [Fact]
    public async Task Bei_zwei_gleichzeitigen_Buchungen_auf_denselben_Sitzplatz_gewinnt_genau_ein_Versuch()
    {
        var ct = TestContext.Current.CancellationToken;
        var (eventId, priceCategoryId) = await SeedEventAsync(ct);

        var commandEins = new CreateBookingCommand(eventId, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionCommand("A", 1, priceCategoryId)]);
        var commandZwei = new CreateBookingCommand(eventId, "Max Mustermann", "max@example.com",
            [new CreateBookingPositionCommand("A", 1, priceCategoryId)]);

        await using var dbEins = _database.CreateContext();
        await using var dbZwei = _database.CreateContext();
        var serviceEins = new BookingService(dbEins);
        var serviceZwei = new BookingService(dbZwei);

        var taskEins = serviceEins.CreateBookingAsync(commandEins, ct);
        var taskZwei = serviceZwei.CreateBookingAsync(commandZwei, ct);
        var ergebnisse = await Task.WhenAll(taskEins, taskZwei);

        Assert.Single(ergebnisse, r => r is BookingCreateResult.Success);
        var konflikt = Assert.Single(ergebnisse.OfType<BookingCreateResult.SeatConflict>());
        Assert.Contains("A1", konflikt.ConflictingSeats);

        await using var verify = _database.CreateContext();
        var belegteSitzplaetze = await verify.BookingPositions.CountAsync(
            bp => bp.EventId == eventId && bp.RowLabel == "A" && bp.ColumnNumber == 1 && bp.CancelledAtUtc == null, ct);
        Assert.Equal(1, belegteSitzplaetze); // genau einmal belegt, kein Doppel-Insert, keine Teilbuchung
    }

    [Fact]
    public async Task Bei_zwei_gleichzeitigen_Mehrsitz_Buchungen_mit_gegenlaeufiger_Reihenfolge_auf_dieselben_Sitzplaetze_gewinnt_genau_ein_Versuch()
    {
        // Beide Versuche fragen dieselben zwei Sitzplätze an, aber in genau entgegengesetzter Reihenfolge
        // ("gegenläufige Insert-Reihenfolge", siehe BookingService.cs:101-106) — beweist, dass die
        // deterministische Sortierung nach RowLabel/ColumnNumber (AD-1) auch bei Mehrsitz-Buchungen
        // greift und keinen Deadlock erzeugt, statt nur den bereits durch den ersten Test bewiesenen
        // Einzelsitzplatz-Fall.
        var ct = TestContext.Current.CancellationToken;
        var (eventId, priceCategoryId) = await SeedEventAsync(ct);

        var commandEins = new CreateBookingCommand(eventId, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionCommand("A", 1, priceCategoryId), new CreateBookingPositionCommand("A", 2, priceCategoryId)]);
        var commandZwei = new CreateBookingCommand(eventId, "Max Mustermann", "max@example.com",
            [new CreateBookingPositionCommand("A", 2, priceCategoryId), new CreateBookingPositionCommand("A", 1, priceCategoryId)]);

        await using var dbEins = _database.CreateContext();
        await using var dbZwei = _database.CreateContext();
        var serviceEins = new BookingService(dbEins);
        var serviceZwei = new BookingService(dbZwei);

        var taskEins = serviceEins.CreateBookingAsync(commandEins, ct);
        var taskZwei = serviceZwei.CreateBookingAsync(commandZwei, ct);
        var ergebnisse = await Task.WhenAll(taskEins, taskZwei);

        Assert.Single(ergebnisse, r => r is BookingCreateResult.Success);
        var konflikt = Assert.Single(ergebnisse.OfType<BookingCreateResult.SeatConflict>());
        Assert.NotEmpty(konflikt.ConflictingSeats);

        await using var verify = _database.CreateContext();
        var belegteSitzplaetze = await verify.BookingPositions.CountAsync(
            bp => bp.EventId == eventId && bp.RowLabel == "A" && (bp.ColumnNumber == 1 || bp.ColumnNumber == 2) && bp.CancelledAtUtc == null, ct);
        Assert.Equal(2, belegteSitzplaetze); // beide Sitzplätze der Gewinner-Buchung belegt, keine Teilbuchung, kein Doppel-Insert
    }

    [Fact]
    public async Task Zwei_gleichzeitige_Buchungen_auf_disjunkte_Sitzplaetze_blockieren_sich_nicht_und_gelingen_beide()
    {
        var ct = TestContext.Current.CancellationToken;
        var (eventId, priceCategoryId) = await SeedEventAsync(ct);

        var commandEins = new CreateBookingCommand(eventId, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionCommand("A", 1, priceCategoryId)]);
        var commandZwei = new CreateBookingCommand(eventId, "Max Mustermann", "max@example.com",
            [new CreateBookingPositionCommand("A", 2, priceCategoryId)]);

        await using var dbEins = _database.CreateContext();
        await using var dbZwei = _database.CreateContext();
        var serviceEins = new BookingService(dbEins);
        var serviceZwei = new BookingService(dbZwei);

        var ergebnisse = await Task.WhenAll(
            serviceEins.CreateBookingAsync(commandEins, ct),
            serviceZwei.CreateBookingAsync(commandZwei, ct));

        Assert.All(ergebnisse, r => Assert.IsType<BookingCreateResult.Success>(r));
    }
}
