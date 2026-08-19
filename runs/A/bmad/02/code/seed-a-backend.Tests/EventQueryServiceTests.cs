using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class EventQueryServiceTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task GetEventSummariesAsync_liefert_korrekten_Venue_Namen_ueber_zwei_Spielstaetten_aufsteigend_sortiert()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venueNord = new Venue { Name = "Spielstätte Nord" };
        var raumNord1 = new Room { Name = "Saal 1", RowLabels = ["A", "B"], ColumnCount = 4, AisleColumns = [] };
        var raumNord2 = new Room { Name = "Saal 2", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueNord.Rooms.Add(raumNord1);
        venueNord.Rooms.Add(raumNord2);

        var venueSued = new Venue { Name = "Spielstätte Süd" };
        var raumSued = new Room { Name = "Konzertsaal", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueSued.Rooms.Add(raumSued);

        db.Venues.AddRange(venueNord, venueSued);
        await db.SaveChangesAsync(ct);

        var spaeteVeranstaltung = new Event
        {
            RoomId = raumNord1.Id,
            Titel = "Spätes Konzert",
            Beschreibung = "...",
            Zeitpunkt = new DateTime(2026, 9, 20, 20, 0, 0),
            DauerMinuten = 90,
            Altersfreigabe = 0,
        };
        var fruehesEreignis = new Event
        {
            RoomId = raumSued.Id,
            Titel = "Frühe Matinee",
            Beschreibung = "...",
            Zeitpunkt = new DateTime(2026, 9, 10, 11, 0, 0),
            DauerMinuten = 60,
            Altersfreigabe = 0,
        };
        var mittleresEreignis = new Event
        {
            RoomId = raumNord2.Id,
            Titel = "Mittleres Stück",
            Beschreibung = "...",
            Zeitpunkt = new DateTime(2026, 9, 15, 19, 0, 0),
            DauerMinuten = 100,
            Altersfreigabe = 0,
        };
        db.Events.AddRange(spaeteVeranstaltung, fruehesEreignis, mittleresEreignis);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: null, bis: null, venueId: null, ct);

        Assert.Collection(result,
            s => { Assert.Equal("Frühe Matinee", s.Titel); Assert.Equal("Spielstätte Süd", s.Spielstaette); },
            s => { Assert.Equal("Mittleres Stück", s.Titel); Assert.Equal("Spielstätte Nord", s.Spielstaette); },
            s => { Assert.Equal("Spätes Konzert", s.Titel); Assert.Equal("Spielstätte Nord", s.Spielstaette); });
    }

    [Fact]
    public async Task GetEventSummariesAsync_gegen_leere_Datenbank_liefert_leere_Liste()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var service = new EventQueryService(db);

        var result = await service.GetEventSummariesAsync(von: null, bis: null, venueId: null, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_nur_von_liefert_nur_Veranstaltungen_an_oder_nach_diesem_Datum()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Testspielstätte" };
        var raum = new Room { Name = "Saal", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var davor = NeuesEvent(raum.Id, "Davor", new DateTime(2026, 9, 9, 20, 0, 0));
        var amTag = NeuesEvent(raum.Id, "Am Tag", new DateTime(2026, 9, 10, 0, 0, 0));
        var danach = NeuesEvent(raum.Id, "Danach", new DateTime(2026, 9, 11, 20, 0, 0));
        db.Events.AddRange(davor, amTag, danach);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: new DateOnly(2026, 9, 10), bis: null, venueId: null, ct);

        Assert.Collection(result,
            s => Assert.Equal("Am Tag", s.Titel),
            s => Assert.Equal("Danach", s.Titel));
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_nur_bis_liefert_nur_Veranstaltungen_an_oder_vor_diesem_Datum()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Testspielstätte" };
        var raum = new Room { Name = "Saal", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var davor = NeuesEvent(raum.Id, "Davor", new DateTime(2026, 9, 9, 20, 0, 0));
        var amTag = NeuesEvent(raum.Id, "Am Tag", new DateTime(2026, 9, 10, 20, 0, 0));
        var danach = NeuesEvent(raum.Id, "Danach", new DateTime(2026, 9, 11, 20, 0, 0));
        db.Events.AddRange(davor, amTag, danach);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: null, bis: new DateOnly(2026, 9, 10), venueId: null, ct);

        Assert.Collection(result,
            s => Assert.Equal("Davor", s.Titel),
            s => Assert.Equal("Am Tag", s.Titel));
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_von_und_bis_liefert_nur_Veranstaltungen_im_Bereich_inklusive_beider_Enden()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Testspielstätte" };
        var raum = new Room { Name = "Saal", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var davor = NeuesEvent(raum.Id, "Davor", new DateTime(2026, 9, 4, 20, 0, 0));
        var vonTag = NeuesEvent(raum.Id, "Von-Tag", new DateTime(2026, 9, 5, 0, 0, 0));
        var bisTagSpaet = NeuesEvent(raum.Id, "Bis-Tag spät", new DateTime(2026, 9, 12, 20, 0, 0));
        var danach = NeuesEvent(raum.Id, "Danach", new DateTime(2026, 9, 13, 0, 0, 0));
        db.Events.AddRange(davor, vonTag, bisTagSpaet, danach);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: new DateOnly(2026, 9, 5), bis: new DateOnly(2026, 9, 12), venueId: null, ct);

        Assert.Collection(result,
            s => Assert.Equal("Von-Tag", s.Titel),
            s => Assert.Equal("Bis-Tag spät", s.Titel));
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_bis_gleich_DateOnly_MaxValue_wirft_nicht_und_liefert_das_Ereignis()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venue = new Venue { Name = "Testspielstätte" };
        var raum = new Room { Name = "Saal", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venue.Rooms.Add(raum);
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        var event_ = NeuesEvent(raum.Id, "Weit in der Zukunft", new DateTime(2026, 9, 10, 20, 0, 0));
        db.Events.Add(event_);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: null, bis: DateOnly.MaxValue, venueId: null, ct);

        Assert.Collection(result, s => Assert.Equal("Weit in der Zukunft", s.Titel));
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_venueId_liefert_nur_Veranstaltungen_dieser_Spielstaette()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venueNord = new Venue { Name = "Spielstätte Nord" };
        var raumNord = new Room { Name = "Saal Nord", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueNord.Rooms.Add(raumNord);

        var venueSued = new Venue { Name = "Spielstätte Süd" };
        var raumSued = new Room { Name = "Saal Süd", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueSued.Rooms.Add(raumSued);

        db.Venues.AddRange(venueNord, venueSued);
        await db.SaveChangesAsync(ct);

        var eventNord = NeuesEvent(raumNord.Id, "Konzert Nord", new DateTime(2026, 9, 10, 20, 0, 0));
        var eventSued = NeuesEvent(raumSued.Id, "Konzert Süd", new DateTime(2026, 9, 11, 20, 0, 0));
        db.Events.AddRange(eventNord, eventSued);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(von: null, bis: null, venueId: venueNord.Id, ct);

        Assert.Collection(result, s => Assert.Equal("Konzert Nord", s.Titel));
    }

    [Fact]
    public async Task GetEventSummariesAsync_mit_venueId_und_Datumsbereich_liefert_nur_Veranstaltungen_die_beide_Kriterien_erfuellen()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venueNord = new Venue { Name = "Spielstätte Nord" };
        var raumNord = new Room { Name = "Saal Nord", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueNord.Rooms.Add(raumNord);

        var venueSued = new Venue { Name = "Spielstätte Süd" };
        var raumSued = new Room { Name = "Saal Süd", RowLabels = ["A"], ColumnCount = 4, AisleColumns = [] };
        venueSued.Rooms.Add(raumSued);

        db.Venues.AddRange(venueNord, venueSued);
        await db.SaveChangesAsync(ct);

        var falscheSpielstaetteImZeitraum = NeuesEvent(raumSued.Id, "Falsche Spielstätte im Zeitraum", new DateTime(2026, 9, 10, 20, 0, 0));
        var richtigeSpielstaetteAusserhalbZeitraum = NeuesEvent(raumNord.Id, "Richtige Spielstätte außerhalb Zeitraum", new DateTime(2026, 9, 20, 20, 0, 0));
        var richtigeSpielstaetteImZeitraum = NeuesEvent(raumNord.Id, "Richtige Spielstätte im Zeitraum", new DateTime(2026, 9, 10, 20, 0, 0));
        db.Events.AddRange(falscheSpielstaetteImZeitraum, richtigeSpielstaetteAusserhalbZeitraum, richtigeSpielstaetteImZeitraum);
        await db.SaveChangesAsync(ct);

        var service = new EventQueryService(db);
        var result = await service.GetEventSummariesAsync(
            von: new DateOnly(2026, 9, 9), bis: new DateOnly(2026, 9, 11), venueId: venueNord.Id, ct);

        Assert.Collection(result, s => Assert.Equal("Richtige Spielstätte im Zeitraum", s.Titel));
    }

    private static Event NeuesEvent(int roomId, string titel, DateTime zeitpunkt) => new()
    {
        RoomId = roomId,
        Titel = titel,
        Beschreibung = "...",
        Zeitpunkt = zeitpunkt,
        DauerMinuten = 60,
        Altersfreigabe = 0,
    };
}
