using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Tests;

public class BuchungServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static BuchungCreateRequestDto Anfrage(params (string reihe, int spalte, string kategorieId)[] plaetze) =>
        new(
            "E1",
            "Erika Mustermann",
            "erika@example.com",
            plaetze.Select(p => new BuchungPositionRequestDto(p.reihe, p.spalte, p.kategorieId)).ToList()
        );

    [Fact]
    public async Task ErstelleBuchung_BerechnetGesamtpreisAusPreiskategorien()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        var buchung = await service.ErstelleBuchungAsync(
            Anfrage(("A", 1, "E1-A"), ("A", 2, "E1-B")), TestContext.Current.CancellationToken);

        Assert.Equal(50m, buchung.Gesamtpreis);
        Assert.Equal(2, buchung.Positionen.Count);
        Assert.Equal(8, buchung.Referenz.Length);
    }

    [Fact]
    public async Task ErstelleBuchung_LehntBelegtenSitzplatzVollstaendigAb()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        await service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A")), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<SitzplatzKonfliktException>(() =>
            service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A"), ("A", 2, "E1-B")), TestContext.Current.CancellationToken));

        using var kontrolle = _factory.CreateContext();
        // Der zweite, abgelehnte Versuch darf keine Teilbuchung hinterlassen: A2 bleibt frei.
        Assert.Equal(1, kontrolle.Buchungspositionen.Count(p => p.Status == Api.Models.BuchungStatus.Aktiv));
    }

    [Fact]
    public async Task ErstelleBuchung_ZweiUnabhaengigeSitzungenAufDenselbenSitzplatz_NurEineGewinnt()
    {
        // Simuliert zwei gleichzeitige Besucher (je eigener DbContext, wie bei zwei
        // parallelen HTTP-Requests): Sitzung B committet zuerst; Sitzung A hatte den
        // Sitzplatz bereits ausgewählt, bevor B abgeschlossen hat, und muss beim
        // Speichern vollständig abgelehnt werden (A-F13), nicht nur teilweise.
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);

        using var dbA = _factory.CreateContext();
        using var dbB = _factory.CreateContext();
        var serviceA = new BuchungService(dbA);
        var serviceB = new BuchungService(dbB);

        var buchungB = await serviceB.ErstelleBuchungAsync(Anfrage(("B", 1, "E1-A")), TestContext.Current.CancellationToken);
        Assert.Equal("Aktiv", buchungB.Status);

        await Assert.ThrowsAsync<SitzplatzKonfliktException>(() =>
            serviceA.ErstelleBuchungAsync(Anfrage(("B", 1, "E1-A"), ("B", 2, "E1-A")), TestContext.Current.CancellationToken));

        using var kontrolle = _factory.CreateContext();
        Assert.Equal(1, kontrolle.Buchungspositionen.Count(p => p.Status == Api.Models.BuchungStatus.Aktiv));
    }

    [Fact]
    public async Task Stornieren_GibtSitzplatzWiederFrei()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        var buchung = await service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A")), TestContext.Current.CancellationToken);
        var storniert = await service.StornierenAsync(buchung.Referenz, TestContext.Current.CancellationToken);
        Assert.Equal("Storniert", storniert.Status);

        var neueBuchung = await service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A")), TestContext.Current.CancellationToken);
        Assert.Equal("Aktiv", neueBuchung.Status);
    }

    [Fact]
    public async Task Stornieren_BereitsStornierterBuchung_WirftFehler()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        var buchung = await service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A")), TestContext.Current.CancellationToken);
        await service.StornierenAsync(buchung.Referenz, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BuchungBereitsStorniertException>(() =>
            service.StornierenAsync(buchung.Referenz, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HoleBuchung_UnbekannteReferenz_WirftNichtGefundenFehler()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        await Assert.ThrowsAsync<BuchungNichtGefundenException>(() =>
            service.HoleBuchungAsync("UNBEKANN", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErstelleBuchung_GangAlsSitzplatz_WirftValidierungsfehler()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        // Spalte 3 ist im Testraum als Gang definiert.
        await Assert.ThrowsAsync<UngueltigeBuchungException>(() =>
            service.ErstelleBuchungAsync(Anfrage(("A", 3, "E1-A")), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErstelleBuchung_DoppelteAuswahlDesselbenSitzplatzes_WirftValidierungsfehler()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        await Assert.ThrowsAsync<UngueltigeBuchungException>(() =>
            service.ErstelleBuchungAsync(Anfrage(("A", 1, "E1-A"), ("A", 1, "E1-B")), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErstelleBuchung_UnbekannteVeranstaltung_WirftNichtGefundenFehler()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);
        var service = new BuchungService(db);

        var anfrage = new BuchungCreateRequestDto(
            "UNBEKANNT", "Erika", "erika@example.com",
            [new BuchungPositionRequestDto("A", 1, "E1-A")]);

        await Assert.ThrowsAsync<VeranstaltungNichtGefundenException>(() =>
            service.ErstelleBuchungAsync(anfrage, TestContext.Current.CancellationToken));
    }
}
