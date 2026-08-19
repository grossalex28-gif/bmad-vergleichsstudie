using seed_a_backend.Api.Application;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class EventDetailServiceTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task GetEventDetailAsync_liefert_alle_Angaben_inkl_Preiskategorien()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Spielstätte Nord" };
        var raum = new Room { Name = "Kleiner Saal", RowLabels = ["A", "B"], ColumnCount = 10, AisleColumns = [6] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var veranstaltung = new Event
        {
            RoomId = raum.Id,
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein intimes Konzert mit klassischen Stücken.",
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0),
            DauerMinuten = 90,
            Altersfreigabe = 0,
        };
        db.Events.Add(veranstaltung);
        await db.SaveChangesAsync(ct);

        db.PriceCategories.AddRange(
            new PriceCategory { EventId = veranstaltung.Id, Name = "Kategorie A", Preis = 32.00m },
            new PriceCategory { EventId = veranstaltung.Id, Name = "Kategorie B", Preis = 22.00m });
        await db.SaveChangesAsync(ct);

        var service = new EventDetailService(db);
        var result = await service.GetEventDetailAsync(veranstaltung.Id, ct);

        Assert.NotNull(result);
        Assert.Equal(veranstaltung.Id, result!.Id);
        Assert.Equal("Kammerkonzert Frühling", result.Titel);
        Assert.Equal("Ein intimes Konzert mit klassischen Stücken.", result.Beschreibung);
        Assert.Equal(90, result.DauerMinuten);
        Assert.Equal(0, result.Altersfreigabe);
        Assert.Equal("Spielstätte Nord", result.Spielstaette);
        Assert.Equal("Kleiner Saal", result.Raum);
        Assert.Equal(new DateTime(2026, 9, 5, 19, 30, 0), result.Zeitpunkt);
        Assert.Collection(result.Preiskategorien,
            pc => { Assert.Equal("Kategorie A", pc.Name); Assert.Equal(32.00m, pc.Preis); },
            pc => { Assert.Equal("Kategorie B", pc.Name); Assert.Equal(22.00m, pc.Preis); });
    }

    [Fact]
    public async Task GetEventDetailAsync_mit_nicht_existierender_Id_liefert_null()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var service = new EventDetailService(db);

        var result = await service.GetEventDetailAsync(999999, ct);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEventDetailAsync_ohne_Preiskategorien_liefert_leere_Liste()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Spielstätte Nord" };
        var raum = new Room { Name = "Kleiner Saal", RowLabels = ["A", "B"], ColumnCount = 10, AisleColumns = [6] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var veranstaltung = new Event
        {
            RoomId = raum.Id,
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein intimes Konzert mit klassischen Stücken.",
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0),
            DauerMinuten = 90,
            Altersfreigabe = 0,
        };
        db.Events.Add(veranstaltung);
        await db.SaveChangesAsync(ct);

        var service = new EventDetailService(db);
        var result = await service.GetEventDetailAsync(veranstaltung.Id, ct);

        Assert.NotNull(result);
        Assert.Empty(result!.Preiskategorien);
    }
}
