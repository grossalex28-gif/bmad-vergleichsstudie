using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

/// <summary>
/// Verifiziert die per DB-CHECK-Constraint erzwungene Invariante, dass `BookingPosition.BookingId`
/// und `PriceCategoryId` immer gemeinsam null oder gemeinsam gesetzt sind (AD-2), statt sie nur
/// über einen XML-Doc-Kommentar zu dokumentieren.
/// </summary>
public class BookingPositionInvariantTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    /// <summary>
    /// Prüft nicht nur den Exception-Typ, sondern dass die `DbUpdateException` tatsächlich vom
    /// erwarteten CHECK-Constraint stammt (SQL-Server-Fehlernummer 547 für CHECK-/FK-Verletzungen,
    /// plus Constraint-Name in der Meldung) — sonst würde auch eine zufällige `DbUpdateException`
    /// aus einer anderen Ursache (z. B. FK- oder Unique-Index-Verletzung) den Test bestehen lassen.
    /// </summary>
    private static async Task AssertViolatesCheckConstraintAsync(Func<Task> saveChanges)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(saveChanges);
        var sqlException = Assert.IsType<SqlException>(ex.InnerException);
        Assert.Equal(547, sqlException.Number);
        Assert.Contains(AppDbContext.BookingPositionBeideNullOderBeideGesetztConstraintName, sqlException.Message);
    }

    private static async Task<(int EventId, int PriceCategoryId, int BookingId)> SeedGraphAsync(AppDbContext db, CancellationToken ct)
    {
        var @event = new Event { Titel = "Testkonzert", Beschreibung = "Beschreibung", Zeitpunkt = DateTime.UtcNow, DauerMinuten = 90, Altersfreigabe = 0 };
        var room = new Room { Name = "Saal 1", RowLabels = ["A"], ColumnCount = 5, Events = [@event] };
        var venue = new Venue { Name = "Testhalle", Rooms = [room] };
        var priceCategory = new PriceCategory { Name = "Standard", Preis = 10m };
        @event.PriceCategories.Add(priceCategory);
        var booking = new Booking { Reference = "ABCD2345", Name = "Erika Musterfrau", Email = "erika@example.com" };

        db.Venues.Add(venue);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        return (@event.Id, priceCategory.Id, booking.Id);
    }

    [Fact]
    public async Task BookingId_gesetzt_ohne_PriceCategoryId_verletzt_die_Check_Constraint()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var (eventId, _, bookingId) = await SeedGraphAsync(db, ct);

        db.BookingPositions.Add(new BookingPosition
        {
            EventId = eventId,
            RowLabel = "A",
            ColumnNumber = 1,
            BookingId = bookingId,
            PriceCategoryId = null,
        });

        await AssertViolatesCheckConstraintAsync(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task PriceCategoryId_gesetzt_ohne_BookingId_verletzt_die_Check_Constraint()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var (eventId, priceCategoryId, _) = await SeedGraphAsync(db, ct);

        db.BookingPositions.Add(new BookingPosition
        {
            EventId = eventId,
            RowLabel = "A",
            ColumnNumber = 1,
            BookingId = null,
            PriceCategoryId = priceCategoryId,
        });

        await AssertViolatesCheckConstraintAsync(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task BookingId_und_PriceCategoryId_gemeinsam_gesetzt_wird_gespeichert()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var (eventId, priceCategoryId, bookingId) = await SeedGraphAsync(db, ct);

        db.BookingPositions.Add(new BookingPosition
        {
            EventId = eventId,
            RowLabel = "A",
            ColumnNumber = 1,
            BookingId = bookingId,
            PriceCategoryId = priceCategoryId,
        });
        await db.SaveChangesAsync(ct);

        var gespeichert = await db.BookingPositions.SingleAsync(
            bp => bp.EventId == eventId && bp.RowLabel == "A" && bp.ColumnNumber == 1, ct);
        Assert.Equal(bookingId, gespeichert.BookingId);
        Assert.Equal(priceCategoryId, gespeichert.PriceCategoryId);
    }
}
