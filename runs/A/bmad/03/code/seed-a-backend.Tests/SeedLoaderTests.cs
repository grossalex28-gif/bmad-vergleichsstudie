using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class SeedLoaderTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    private static string AnfangsdatenbestandPfad =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "anfangsdatenbestand.json");

    public async ValueTask InitializeAsync()
    {
        _db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(SeedLoaderTests));
    }

    public async ValueTask DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task SeedAsync_BefuelltLeereDatenbankMitErwartetenZaehlwerten()
    {
        await SeedLoader.SeedAsync(_db, AnfangsdatenbestandPfad);

        Assert.Equal(2, await _db.Spielstaetten.CountAsync());
        Assert.Equal(4, await _db.Raeume.CountAsync());
        Assert.Equal(6, await _db.Veranstaltungen.CountAsync());
        Assert.Equal(12, await _db.Preiskategorien.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ZweiterAufrufAufBefuellterDatenbank_ErzeugtKeineDuplikate()
    {
        await SeedLoader.SeedAsync(_db, AnfangsdatenbestandPfad);
        await SeedLoader.SeedAsync(_db, AnfangsdatenbestandPfad);

        Assert.Equal(2, await _db.Spielstaetten.CountAsync());
        Assert.Equal(4, await _db.Raeume.CountAsync());
        Assert.Equal(6, await _db.Veranstaltungen.CountAsync());
        Assert.Equal(12, await _db.Preiskategorien.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_OffsetloserZeitpunktImAnfangsdatenbestand_WirdAlsEuropaBerlinZeitInterpretiert()
    {
        await SeedLoader.SeedAsync(_db, AnfangsdatenbestandPfad);

        var veranstaltung = await _db.Veranstaltungen.FindAsync("E1");

        Assert.NotNull(veranstaltung);
        Assert.Equal(TimeSpan.FromHours(2), veranstaltung.Zeitpunkt.Offset);
    }

    [Fact]
    public async Task SeedAsync_BefuelltSitzplatzbelegungAusBelegteSitzplaetze()
    {
        await SeedLoader.SeedAsync(_db, AnfangsdatenbestandPfad);

        Assert.Equal(3, await _db.Sitzplatzbelegungen.CountAsync());
    }
}
