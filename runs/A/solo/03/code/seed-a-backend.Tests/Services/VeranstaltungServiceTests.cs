using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Tests.Services;

[Collection("Datenbank")]
public class VeranstaltungServiceTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task GetVeranstaltungenAsync_FiltertNachSpielstaette()
    {
        await using var context = fixture.CreateContext();
        var service = new VeranstaltungService(context);
        var eigeneSpielstaette = await TestDataFactory.SeedVeranstaltungAsync(context);
        var andereSpielstaette = await TestDataFactory.SeedVeranstaltungAsync(context);

        var ergebnis = await service.GetVeranstaltungenAsync(null, null, eigeneSpielstaette.SpielstaetteId);

        Assert.Contains(ergebnis, v => v.Id == eigeneSpielstaette.VeranstaltungId);
        Assert.DoesNotContain(ergebnis, v => v.Id == andereSpielstaette.VeranstaltungId);
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_FiltertNachDatumsbereich()
    {
        await using var context = fixture.CreateContext();
        var service = new VeranstaltungService(context);
        var innerhalb = await TestDataFactory.SeedVeranstaltungAsync(context, new DateTime(2027, 5, 10, 20, 0, 0, DateTimeKind.Utc));
        var ausserhalb = await TestDataFactory.SeedVeranstaltungAsync(context, new DateTime(2027, 7, 1, 20, 0, 0, DateTimeKind.Utc));

        var ergebnis = await service.GetVeranstaltungenAsync(
            new DateTime(2027, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            null);

        Assert.Contains(ergebnis, v => v.Id == innerhalb.VeranstaltungId);
        Assert.DoesNotContain(ergebnis, v => v.Id == ausserhalb.VeranstaltungId);
    }

    [Fact]
    public async Task GetSitzplanAsync_MarkiertGangUndFreieSitzplaetzeKorrekt()
    {
        await using var context = fixture.CreateContext();
        var service = new VeranstaltungService(context);
        var daten = await TestDataFactory.SeedVeranstaltungAsync(context);

        var sitzplan = await service.GetSitzplanAsync(daten.VeranstaltungId);

        var gang = sitzplan.Sitzplaetze.Where(s => s.Spalte == 3).ToList();
        Assert.All(gang, s => Assert.Equal(SitzplatzTyp.Gang, s.Typ));
        Assert.All(gang, s => Assert.Null(s.Status));

        var sitzplaetze = sitzplan.Sitzplaetze.Where(s => s.Spalte != 3).ToList();
        Assert.All(sitzplaetze, s => Assert.Equal(SitzplatzTyp.Sitzplatz, s.Typ));
        Assert.All(sitzplaetze, s => Assert.Equal(SitzplatzStatus.Frei, s.Status));
        Assert.Equal(3 * 4, sitzplaetze.Count); // 3 Reihen x (5 Spalten - 1 Gang)
    }

    [Fact]
    public async Task GetDetailAsync_UnbekannteVeranstaltung_WirftNotFound()
    {
        await using var context = fixture.CreateContext();
        var service = new VeranstaltungService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetDetailAsync("UNBEKANNT"));
    }
}
