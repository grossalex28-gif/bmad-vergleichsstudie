using seed_a_backend.Api.Application;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class SeatMapServiceTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task GetSeatMapAsync_liefert_grid_mit_frei_belegt_und_gang_status()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Spielstätte Nord" };
        var raum = new Room { Name = "Kleiner Saal", RowLabels = ["A", "B"], ColumnCount = 4, AisleColumns = [3] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var veranstaltung = new Event
        {
            RoomId = raum.Id,
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein intimes Konzert.",
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0),
            DauerMinuten = 90,
            Altersfreigabe = 0,
        };
        db.Events.Add(veranstaltung);
        await db.SaveChangesAsync(ct);

        db.BookingPositions.Add(new BookingPosition
        {
            EventId = veranstaltung.Id,
            RowLabel = "A",
            ColumnNumber = 2,
            BookingId = null,
            PriceCategoryId = null,
            CancelledAtUtc = null,
        });
        await db.SaveChangesAsync(ct);

        var service = new SeatMapService(db);
        var result = await service.GetSeatMapAsync(veranstaltung.Id, ct);

        Assert.NotNull(result);
        Assert.Equal(["A", "B"], result!.RowLabels);
        Assert.Equal(4, result.ColumnCount);
        Assert.Equal([3], result.AisleColumns);

        var rowA = result.Rows.Single(r => r.RowLabel == "A");
        Assert.Equal("free", rowA.Cells.Single(c => c.ColumnNumber == 1).Status);
        Assert.Equal("occupied", rowA.Cells.Single(c => c.ColumnNumber == 2).Status);
        Assert.Equal("aisle", rowA.Cells.Single(c => c.ColumnNumber == 3).Status);
        Assert.Equal("free", rowA.Cells.Single(c => c.ColumnNumber == 4).Status);

        var rowB = result.Rows.Single(r => r.RowLabel == "B");
        Assert.Equal("aisle", rowB.Cells.Single(c => c.ColumnNumber == 3).Status);
        Assert.All(rowB.Cells.Where(c => c.ColumnNumber != 3), c => Assert.Equal("free", c.Status));
    }

    [Fact]
    public async Task GetSeatMapAsync_ignoriert_stornierte_BookingPosition_und_zeigt_den_Sitzplatz_als_frei()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Spielstätte Nord" };
        var raum = new Room { Name = "Kleiner Saal", RowLabels = ["A"], ColumnCount = 2, AisleColumns = [] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var veranstaltung = new Event
        {
            RoomId = raum.Id,
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein intimes Konzert.",
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0),
            DauerMinuten = 90,
            Altersfreigabe = 0,
        };
        db.Events.Add(veranstaltung);
        await db.SaveChangesAsync(ct);

        db.BookingPositions.Add(new BookingPosition
        {
            EventId = veranstaltung.Id,
            RowLabel = "A",
            ColumnNumber = 1,
            BookingId = null,
            PriceCategoryId = null,
            CancelledAtUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync(ct);

        var service = new SeatMapService(db);
        var result = await service.GetSeatMapAsync(veranstaltung.Id, ct);

        Assert.NotNull(result);
        var rowA = result!.Rows.Single(r => r.RowLabel == "A");
        Assert.Equal("free", rowA.Cells.Single(c => c.ColumnNumber == 1).Status);
    }

    [Fact]
    public async Task GetSeatMapAsync_mit_nicht_existierender_Id_liefert_null()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var service = new SeatMapService(db);

        var result = await service.GetSeatMapAsync(999999, ct);

        Assert.Null(result);
    }
}
