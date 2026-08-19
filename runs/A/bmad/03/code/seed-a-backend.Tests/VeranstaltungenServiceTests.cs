using seed_a_backend.Api.Data;
using seed_a_backend.Api.Models;
using seed_a_backend.Api.Services;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class VeranstaltungenServiceTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        _db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(VeranstaltungenServiceTests));
    }

    public async ValueTask DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_AufLeererDatenbank_LiefertLeereListe()
    {
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync();

        Assert.Empty(ergebnis);
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_SortiertAufsteigendNachZeitpunkt()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Spalten = 4 });
        _db.Veranstaltungen.AddRange(
            new Veranstaltung
            {
                Id = "E-spaeter",
                Titel = "Später",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero)
            },
            new Veranstaltung
            {
                Id = "E-frueher",
                Titel = "Früher",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero)
            });
        await _db.SaveChangesAsync();

        var service = new VeranstaltungenService(_db);
        var ergebnis = await service.GetVeranstaltungenAsync();

        Assert.Equal(["E-frueher", "E-spaeter"], ergebnis.Select(v => v.Id));
    }

    private async Task SeedDreiVeranstaltungenAsync()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Spalten = 4 });
        _db.Veranstaltungen.AddRange(
            new Veranstaltung
            {
                Id = "E-vor-bereich",
                Titel = "Vor dem Bereich",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 8, 31, 20, 0, 0, TimeSpan.FromHours(2))
            },
            new Veranstaltung
            {
                Id = "E-auf-von",
                Titel = "Genau auf von",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 1, 19, 30, 0, TimeSpan.FromHours(2))
            },
            new Veranstaltung
            {
                Id = "E-in-bereich",
                Titel = "In der Mitte",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.FromHours(2))
            },
            new Veranstaltung
            {
                Id = "E-auf-bis",
                Titel = "Genau auf bis",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 30, 21, 0, 0, TimeSpan.FromHours(2))
            },
            new Veranstaltung
            {
                Id = "E-nach-bereich",
                Titel = "Nach dem Bereich",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.FromHours(2))
            });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_MitVonUndBis_LiefertNurTrefferInklusiveGrenzen()
    {
        await SeedDreiVeranstaltungenAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        Assert.Equal(["E-auf-von", "E-in-bereich", "E-auf-bis"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_NurVonGesetzt_LiefertAllesAbDiesemDatum()
    {
        await SeedDreiVeranstaltungenAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(von: new DateOnly(2026, 9, 15));

        Assert.Equal(["E-in-bereich", "E-auf-bis", "E-nach-bereich"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_NurBisGesetzt_LiefertAllesBisZuDiesemDatum()
    {
        await SeedDreiVeranstaltungenAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(bis: new DateOnly(2026, 9, 15));

        Assert.Equal(["E-vor-bereich", "E-auf-von", "E-in-bereich"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_OhneFilter_LiefertAlleVeranstaltungen()
    {
        await SeedDreiVeranstaltungenAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync();

        Assert.Equal(5, ergebnis.Count);
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_FilterOhneTreffer_LiefertLeereListeOhneFehler()
    {
        await SeedDreiVeranstaltungenAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 14));

        Assert.Empty(ergebnis);
    }

    private async Task SeedZweiSpielstaettenMitJeEinerVeranstaltungAsync()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Stadthalle Nordpark" });
        _db.Spielstaetten.Add(new Spielstaette { Id = "V2", Name = "Kulturhaus Südtor" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Großer Saal", SpielstaetteId = "V1", Spalten = 4 });
        _db.Raeume.Add(new Raum { Id = "R2", Name = "Kleiner Saal", SpielstaetteId = "V2", Spalten = 4 });
        _db.Veranstaltungen.AddRange(
            new Veranstaltung
            {
                Id = "E-v1",
                Titel = "Veranstaltung V1",
                Beschreibung = "-",
                RaumId = "R1",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 10, 20, 0, 0, TimeSpan.FromHours(2))
            },
            new Veranstaltung
            {
                Id = "E-v2",
                Titel = "Veranstaltung V2",
                Beschreibung = "-",
                RaumId = "R2",
                DauerMinuten = 60,
                Altersfreigabe = 0,
                Zeitpunkt = new DateTimeOffset(2026, 9, 12, 20, 0, 0, TimeSpan.FromHours(2))
            });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_MitSpielstaetteId_LiefertNurVeranstaltungenDieserSpielstaette()
    {
        await SeedZweiSpielstaettenMitJeEinerVeranstaltungAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(spielstaetteId: "V1");

        Assert.Equal(["E-v1"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_OhneSpielstaetteIdFilter_LiefertVeranstaltungenBeiderSpielstaetten()
    {
        await SeedZweiSpielstaettenMitJeEinerVeranstaltungAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync();

        Assert.Equal(["E-v1", "E-v2"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_UnbekannteSpielstaetteId_LiefertLeereListeOhneFehler()
    {
        await SeedZweiSpielstaettenMitJeEinerVeranstaltungAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(spielstaetteId: "V-unbekannt");

        Assert.Empty(ergebnis);
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_SpielstaetteIdNurLeerzeichen_WirdWieKeinFilterBehandelt()
    {
        await SeedZweiSpielstaettenMitJeEinerVeranstaltungAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(spielstaetteId: "   ");

        Assert.Equal(["E-v1", "E-v2"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungAsync_ExistierendeId_LiefertDetails()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Stadthalle Nordpark" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Kleiner Saal", SpielstaetteId = "V1", Spalten = 4 });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein intimes Konzert mit klassischen und modernen Stücken für ein kleines Ensemble.",
            RaumId = "R1",
            DauerMinuten = 90,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 5, 19, 30, 0, TimeSpan.FromHours(2))
        });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungAsync("E1");

        Assert.NotNull(ergebnis);
        Assert.Equal("E1", ergebnis!.Id);
        Assert.Equal("Kammerkonzert Frühling", ergebnis.Titel);
        Assert.Equal("Ein intimes Konzert mit klassischen und modernen Stücken für ein kleines Ensemble.", ergebnis.Beschreibung);
        Assert.Equal(90, ergebnis.DauerMinuten);
        Assert.Equal(0, ergebnis.Altersfreigabe);
        Assert.Equal("Stadthalle Nordpark", ergebnis.SpielstaetteName);
        Assert.Equal("Kleiner Saal", ergebnis.RaumName);
        Assert.Equal(new DateTimeOffset(2026, 9, 5, 19, 30, 0, TimeSpan.FromHours(2)), ergebnis.Zeitpunkt);
    }

    [Fact]
    public async Task GetVeranstaltungAsync_MitPreiskategorien_LiefertSieSortiertNachId()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Stadthalle Nordpark" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Kleiner Saal", SpielstaetteId = "V1", Spalten = 4 });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 90,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 5, 19, 30, 0, TimeSpan.FromHours(2))
        });
        _db.Preiskategorien.AddRange(
            new Preiskategorie { Id = "E1-B", Name = "Kategorie B", Preis = 22.00m, VeranstaltungId = "E1" },
            new Preiskategorie { Id = "E1-A", Name = "Kategorie A", Preis = 32.00m, VeranstaltungId = "E1" });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungAsync("E1");

        Assert.NotNull(ergebnis);
        Assert.Equal(["E1-A", "E1-B"], ergebnis!.Preiskategorien.Select(p => p.Id));
        Assert.Equal(["Kategorie A", "Kategorie B"], ergebnis.Preiskategorien.Select(p => p.Name));
        Assert.Equal([32.00m, 22.00m], ergebnis.Preiskategorien.Select(p => p.Preis));
    }

    [Fact]
    public async Task GetVeranstaltungAsync_NichtExistierendeId_LiefertNull()
    {
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungAsync("E-unbekannt");

        Assert.Null(ergebnis);
    }

    [Fact]
    public async Task GetVeranstaltungenAsync_DatumsbereichUndSpielstaetteId_WirktAlsUndVerknuepfung()
    {
        await SeedDreiVeranstaltungenAsync();
        _db.Spielstaetten.Add(new Spielstaette { Id = "V2", Name = "Kulturhaus Südtor" });
        _db.Raeume.Add(new Raum { Id = "R2", Name = "Kleiner Saal", SpielstaetteId = "V2", Spalten = 4 });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E-v2-im-bereich",
            Titel = "Veranstaltung in V2, im Datumsbereich",
            Beschreibung = "-",
            RaumId = "R2",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 15, 20, 0, 0, TimeSpan.FromHours(2))
        });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetVeranstaltungenAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), spielstaetteId: "V1");

        Assert.Equal(["E-auf-von", "E-in-bereich", "E-auf-bis"], ergebnis.Select(v => v.Id));
    }

    [Fact]
    public async Task GetSitzplanAsync_NichtExistierendeVeranstaltung_LiefertNull()
    {
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetSitzplanAsync("E-unbekannt");

        Assert.Null(ergebnis);
    }

    [Fact]
    public async Task GetSitzplanAsync_RaumMitMittelgang_MarkiertGangSpalteAlsGangUndUeberspringtSitzplatzCode()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Reihen = ["A", "B"], Spalten = 4, GangSpalten = [2] });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Test",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero)
        });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetSitzplanAsync("E1");

        Assert.NotNull(ergebnis);
        foreach (var reihe in ergebnis!.Reihen)
        {
            var gangPosition = reihe.Positionen.Single(p => p.Spalte == 2);
            Assert.Equal("Gang", gangPosition.Typ);
            Assert.Null(gangPosition.Code);
            Assert.Null(gangPosition.Status);

            var sitzplatzPositionen = reihe.Positionen.Where(p => p.Spalte != 2).ToList();
            Assert.All(sitzplatzPositionen, p => Assert.Equal("Sitzplatz", p.Typ));
            Assert.Equal(
                [$"{reihe.Reihe}1", $"{reihe.Reihe}3", $"{reihe.Reihe}4"],
                sitzplatzPositionen.Select(p => p.Code));
        }
    }

    [Fact]
    public async Task GetSitzplanAsync_RaumOhneGang_AlleSpaltenSindSitzplaetze()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Reihen = ["A"], Spalten = 3, GangSpalten = [] });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Test",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero)
        });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetSitzplanAsync("E1");

        Assert.NotNull(ergebnis);
        Assert.All(ergebnis!.Reihen.SelectMany(r => r.Positionen), p => Assert.Equal("Sitzplatz", p.Typ));
    }

    [Fact]
    public async Task GetSitzplanAsync_BelegteSitzplatzbelegungZeilen_WerdenAlsBelegtMarkiert()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Reihen = ["A", "B"], Spalten = 4, GangSpalten = [] });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Test",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero)
        });
        _db.Sitzplatzbelegungen.AddRange(
            new Sitzplatzbelegung { VeranstaltungId = "E1", SitzplatzCode = "A2", BuchungId = null },
            new Sitzplatzbelegung { VeranstaltungId = "E1", SitzplatzCode = "B3", BuchungId = null });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetSitzplanAsync("E1");

        Assert.NotNull(ergebnis);
        var alleSitzplaetze = ergebnis!.Reihen.SelectMany(r => r.Positionen).Where(p => p.Typ == "Sitzplatz").ToList();
        var belegte = alleSitzplaetze.Where(p => p.Status == "Belegt").Select(p => p.Code);
        var freie = alleSitzplaetze.Where(p => p.Status == "Frei").Select(p => p.Code);
        Assert.Equal(["A2", "B3"], belegte);
        Assert.Equal(["A1", "A3", "A4", "B1", "B2", "B4"], freie);
    }

    [Fact]
    public async Task GetSitzplanAsync_KeineSitzplatzbelegungZeilen_AlleSitzplaetzeSindFrei()
    {
        _db.Spielstaetten.Add(new Spielstaette { Id = "V1", Name = "Testspielstätte" });
        _db.Raeume.Add(new Raum { Id = "R1", Name = "Testraum", SpielstaetteId = "V1", Reihen = ["A"], Spalten = 2, GangSpalten = [] });
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E1",
            Titel = "Test",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 1, 20, 0, 0, TimeSpan.Zero)
        });
        await _db.SaveChangesAsync();
        var service = new VeranstaltungenService(_db);

        var ergebnis = await service.GetSitzplanAsync("E1");

        Assert.NotNull(ergebnis);
        Assert.All(ergebnis!.Reihen.SelectMany(r => r.Positionen), p => Assert.Equal("Frei", p.Status));
    }
}
