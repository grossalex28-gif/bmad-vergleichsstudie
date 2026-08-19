using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Api.Models;
using seed_a_backend.Api.Services;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class BuchungenServiceTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        _db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(BuchungenServiceTests));
    }

    public async ValueTask DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private sealed class ImmerGleicheReferenz(string referenz) : IReferenzGenerator
    {
        public string Naechste() => referenz;
    }

    private async Task SeedVeranstaltungMitZweiPreiskategorienAsync()
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
        _db.Preiskategorien.AddRange(
            new Preiskategorie { Id = "E1-A", Name = "Kategorie A", Preis = 32.00m, VeranstaltungId = "E1" },
            new Preiskategorie { Id = "E1-B", Name = "Kategorie B", Preis = 22.00m, VeranstaltungId = "E1" });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task BuchungAnlegenAsync_GueltigeAuswahl_LegtBuchungPositionenUndBelegungenAn()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("A1", "E1-A"),
                new BuchungspositionRequestDto("A2", "E1-B"),
            ]);

        var ergebnis = await service.BuchungAnlegenAsync("E1", request);

        Assert.Equal(BuchungErgebnisTyp.Erfolgreich, ergebnis.Typ);
        Assert.NotNull(ergebnis.Buchung);
        Assert.Equal(8, ergebnis.Buchung!.Referenz.Length);
        Assert.All(ergebnis.Buchung.Referenz, c => Assert.Contains(c, "0123456789ABCDEFGHJKMNPQRSTVWXYZ"));
        Assert.Equal(54.00m, ergebnis.Buchung.Gesamtpreis);

        Assert.Equal(1, await _db.Buchungen.CountAsync());
        Assert.Equal(2, await _db.Buchungspositionen.CountAsync());
        Assert.Equal(2, await _db.Sitzplatzbelegungen.CountAsync());
    }

    [Fact]
    public async Task BuchungAnlegenAsync_EinPlatzBereitsBelegt_LiefertKonfliktUndLegtKeineTeilbuchungAn()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        _db.Sitzplatzbelegungen.Add(new Sitzplatzbelegung { VeranstaltungId = "E1", SitzplatzCode = "A1", BuchungId = null });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // simuliert einen frischen Request-Context, der die zuvor gesetzte Belegung nicht mehr lokal trackt
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("A1", "E1-A"),
                new BuchungspositionRequestDto("A2", "E1-B"),
            ]);

        var ergebnis = await service.BuchungAnlegenAsync("E1", request);

        Assert.Equal(BuchungErgebnisTyp.SitzplatzKonflikt, ergebnis.Typ);
        Assert.Equal(["A1"], ergebnis.BetroffeneSitzplaetze);

        Assert.Equal(0, await _db.Buchungen.CountAsync());
        Assert.False(await _db.Sitzplatzbelegungen.AnyAsync(s => s.SitzplatzCode == "A2"));
    }

    [Fact]
    public async Task BuchungAnlegenAsync_PreiskategorieGehoertZuAndererVeranstaltung_LiefertPreiskategorieUngueltig()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        _db.Veranstaltungen.Add(new Veranstaltung
        {
            Id = "E2",
            Titel = "Andere Veranstaltung",
            Beschreibung = "-",
            RaumId = "R1",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTimeOffset(2026, 9, 2, 20, 0, 0, TimeSpan.Zero)
        });
        _db.Preiskategorien.Add(new Preiskategorie { Id = "E2-A", Name = "Kategorie A", Preis = 15.00m, VeranstaltungId = "E2" });
        await _db.SaveChangesAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("A1", "E2-A")]);

        var ergebnis = await service.BuchungAnlegenAsync("E1", request);

        Assert.Equal(BuchungErgebnisTyp.PreiskategorieUngueltig, ergebnis.Typ);
        Assert.Equal("E2-A", ergebnis.UngueltigePreiskategorieId);
        Assert.Equal(0, await _db.Buchungen.CountAsync());
    }

    [Fact]
    public async Task BuchungAnlegenAsync_FuenfReferenzKollisionenInFolge_LiefertReferenzErzeugungFehlgeschlagen()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        _db.Buchungen.Add(new Buchung
        {
            Id = Guid.NewGuid().ToString(),
            Referenz = "AAAAAAAA",
            VeranstaltungId = "E1",
            Name = "Bestehend",
            Email = "bestehend@example.com",
            Status = BuchungStatus.Aktiv,
            Gesamtpreis = 32.00m,
        });
        await _db.SaveChangesAsync();
        var service = new BuchungenService(_db, new ImmerGleicheReferenz("AAAAAAAA"));
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("A1", "E1-A")]);

        var ergebnis = await service.BuchungAnlegenAsync("E1", request);

        Assert.Equal(BuchungErgebnisTyp.ReferenzErzeugungFehlgeschlagen, ergebnis.Typ);
        Assert.Equal(1, await _db.Buchungen.CountAsync());
        Assert.Equal(0, await _db.Buchungspositionen.CountAsync());
        Assert.Equal(0, await _db.Sitzplatzbelegungen.CountAsync());
    }

    [Fact]
    public async Task BuchungAnlegenAsync_SitzplatzCodeMehrfachInDerselbenAnfrage_LiefertSitzplatzDuplikat()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("A1", "E1-A"),
                new BuchungspositionRequestDto("A1", "E1-B"),
            ]);

        var ergebnis = await service.BuchungAnlegenAsync("E1", request);

        Assert.Equal(BuchungErgebnisTyp.SitzplatzDuplikat, ergebnis.Typ);
        Assert.Equal(0, await _db.Buchungen.CountAsync());
    }

    [Fact]
    public async Task BuchungAbrufenAsync_ExistierendeReferenz_LiefertBuchungMitVeranstaltungPositionenUndGesamtpreis()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var anlegenErgebnis = await service.BuchungAnlegenAsync(
            "E1",
            new BuchungAnlegenRequestDto(
                "Max Mustermann",
                "max@example.com",
                [new BuchungspositionRequestDto("A1", "E1-A")]));
        var referenz = anlegenErgebnis.Buchung!.Referenz;

        var ergebnis = await service.BuchungAbrufenAsync(referenz);

        Assert.NotNull(ergebnis);
        Assert.Equal("Test", ergebnis!.VeranstaltungTitel);
        Assert.Equal("Testspielstätte", ergebnis.SpielstaetteName);
        Assert.Equal("Testraum", ergebnis.RaumName);
        Assert.Equal("Aktiv", ergebnis.Status);
        Assert.Equal(32.00m, ergebnis.Gesamtpreis);
        var position = Assert.Single(ergebnis.Positionen);
        Assert.Equal("A1", position.SitzplatzCode);
        Assert.Equal("Kategorie A", position.PreiskategorieName);
    }

    [Fact]
    public async Task BuchungAbrufenAsync_UnbekannteReferenz_LiefertNull()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());

        var ergebnis = await service.BuchungAbrufenAsync("ZZZZZZZZ");

        Assert.Null(ergebnis);
    }

    [Fact]
    public async Task BuchungAbrufenAsync_StornierteBuchung_LiefertTrotzdemMitStatusStorniert()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        _db.Buchungen.Add(new Buchung
        {
            Id = Guid.NewGuid().ToString(),
            Referenz = "STORNBBB",
            VeranstaltungId = "E1",
            Name = "Storniert Testperson",
            Email = "storniert@example.com",
            Status = BuchungStatus.Storniert,
            Gesamtpreis = 32.00m,
        });
        await _db.SaveChangesAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());

        var ergebnis = await service.BuchungAbrufenAsync("STORNBBB");

        Assert.NotNull(ergebnis);
        Assert.Equal("Storniert", ergebnis!.Status);
    }

    private AppDbContext OeffneWeiterenContextAsync() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(SqlServerTestDatabase.BuildConnectionString(nameof(BuchungenServiceTests)))
            .Options);

    [Fact]
    public async Task BuchungStornierenAsync_AktiveBuchung_SetztStatusUndLoeschtSitzplatzbelegungen()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var anlegenErgebnis = await service.BuchungAnlegenAsync(
            "E1",
            new BuchungAnlegenRequestDto(
                "Max Mustermann",
                "max@example.com",
                [new BuchungspositionRequestDto("A1", "E1-A")]));
        var referenz = anlegenErgebnis.Buchung!.Referenz;

        var ergebnis = await service.BuchungStornierenAsync(referenz);

        Assert.Equal(StornierungErgebnisTyp.Erfolgreich, ergebnis.Typ);
        Assert.Equal("Storniert", ergebnis.Buchung!.Status);
        Assert.Equal(0, await _db.Sitzplatzbelegungen.CountAsync());
        Assert.Equal(1, await _db.Buchungspositionen.CountAsync());
    }

    [Fact]
    public async Task BuchungStornierenAsync_UnbekannteReferenz_LiefertNichtGefunden()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());

        var ergebnis = await service.BuchungStornierenAsync("ZZZZZZZZ");

        Assert.Equal(StornierungErgebnisTyp.NichtGefunden, ergebnis.Typ);
    }

    [Fact]
    public async Task BuchungStornierenAsync_BereitsStornierteBuchung_LiefertBereitsStorniertOhneZustandsaenderung()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var buchung = new Buchung
        {
            Id = Guid.NewGuid().ToString(),
            Referenz = "STORNCCC",
            VeranstaltungId = "E1",
            Name = "Storniert Testperson",
            Email = "storniert@example.com",
            Status = BuchungStatus.Storniert,
            Gesamtpreis = 32.00m,
        };
        _db.Buchungen.Add(buchung);
        _db.Sitzplatzbelegungen.Add(new Sitzplatzbelegung { VeranstaltungId = "E1", SitzplatzCode = "A1", BuchungId = buchung.Id });
        await _db.SaveChangesAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());

        var ergebnis = await service.BuchungStornierenAsync("STORNCCC");

        Assert.Equal(StornierungErgebnisTyp.BereitsStorniert, ergebnis.Typ);
        Assert.Equal(1, await _db.Sitzplatzbelegungen.CountAsync());
    }

    [Fact]
    public async Task BuchungStornierenAsync_ZweiGleichzeitigeVersucheAufDieselbeBuchung_GenauEinErfolgUndEinBereitsStorniert()
    {
        await SeedVeranstaltungMitZweiPreiskategorienAsync();
        var service = new BuchungenService(_db, new ReferenzGenerator());
        var anlegenErgebnis = await service.BuchungAnlegenAsync(
            "E1",
            new BuchungAnlegenRequestDto(
                "Max Mustermann",
                "max@example.com",
                [new BuchungspositionRequestDto("A1", "E1-A")]));
        var referenz = anlegenErgebnis.Buchung!.Referenz;

        await using var dbA = OeffneWeiterenContextAsync();
        await using var dbB = OeffneWeiterenContextAsync();
        var serviceA = new BuchungenService(dbA, new ReferenzGenerator());
        var serviceB = new BuchungenService(dbB, new ReferenzGenerator());

        var ergebnisse = await Task.WhenAll(
            serviceA.BuchungStornierenAsync(referenz),
            serviceB.BuchungStornierenAsync(referenz));

        Assert.Single(ergebnisse, e => e.Typ == StornierungErgebnisTyp.Erfolgreich);
        Assert.Single(ergebnisse, e => e.Typ == StornierungErgebnisTyp.BereitsStorniert);
        Assert.Equal(0, await _db.Sitzplatzbelegungen.CountAsync());
    }
}
