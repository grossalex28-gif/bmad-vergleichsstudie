using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Tests;

public class SeedDataLoaderTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static string PfadZumEchtenAnfangsdatenbestand()
    {
        var verzeichnis = AppContext.BaseDirectory;
        var pfad = Path.Combine(verzeichnis, "Data", "anfangsdatenbestand.json");
        return File.Exists(pfad)
            ? pfad
            : Path.Combine(verzeichnis, "..", "..", "..", "..", "seed-a-backend.Api", "Data", "anfangsdatenbestand.json");
    }

    [Fact]
    public async Task LoadIfEmptyAsync_LaedtDenEchtenAnfangsdatenbestand()
    {
        using var db = _factory.CreateContext();

        await SeedDataLoader.LoadIfEmptyAsync(db, PfadZumEchtenAnfangsdatenbestand(), TestContext.Current.CancellationToken);

        Assert.True(await db.Spielstaetten.AnyAsync(TestContext.Current.CancellationToken));
        Assert.True(await db.Veranstaltungen.AnyAsync(TestContext.Current.CancellationToken));
        Assert.True(await db.Preiskategorien.AnyAsync(TestContext.Current.CancellationToken));

        var kammerkonzert = await db.Veranstaltungen
            .Include(v => v.Raum)
            .Include(v => v.Buchungspositionen)
            .FirstAsync(v => v.Id == "E1", TestContext.Current.CancellationToken);

        // Laut Anfangsdatenbestand sind für E1 die Plätze B3, B4 und C7 bereits belegt.
        Assert.Equal(3, kammerkonzert.Buchungspositionen.Count(p => p.Status == BuchungStatus.Aktiv));
        Assert.Contains(kammerkonzert.Buchungspositionen, p => p.Reihe == "B" && p.Spalte == 3);
        Assert.True(kammerkonzert.Raum.IstGueltigerSitzplatz("A", 1));
    }

    [Fact]
    public async Task LoadIfEmptyAsync_WennBereitsDatenVorhanden_TutNichts()
    {
        using var db = _factory.CreateContext();
        TestDatenBuilder.ErstelleVeranstaltungMitRaum(db);

        await SeedDataLoader.LoadIfEmptyAsync(db, PfadZumEchtenAnfangsdatenbestand(), TestContext.Current.CancellationToken);

        // Nur die eine, vorher manuell angelegte Veranstaltung ist vorhanden - kein Nachladen.
        Assert.Equal(1, await db.Veranstaltungen.CountAsync(TestContext.Current.CancellationToken));
    }
}
