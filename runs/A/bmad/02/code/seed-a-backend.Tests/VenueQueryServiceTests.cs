using seed_a_backend.Api.Application;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class VenueQueryServiceTests : IAsyncLifetime
{
    private readonly SqlServerTestDatabase _database = new();

    public ValueTask InitializeAsync() => _database.InitializeAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task GetVenuesAsync_liefert_alle_Spielstaetten_alphabetisch_sortiert_nach_Namen()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();

        var venueSued = new Venue { Name = "Spielstätte Süd" };
        var venueNord = new Venue { Name = "Spielstätte Nord" };
        db.Venues.AddRange(venueSued, venueNord);
        await db.SaveChangesAsync(ct);

        var service = new VenueQueryService(db);
        var result = await service.GetVenuesAsync(ct);

        Assert.Collection(result,
            v => Assert.Equal("Spielstätte Nord", v.Name),
            v => Assert.Equal("Spielstätte Süd", v.Name));
    }

    [Fact]
    public async Task GetVenuesAsync_gegen_leere_Datenbank_liefert_leere_Liste()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _database.CreateContext();
        var service = new VenueQueryService(db);

        var result = await service.GetVenuesAsync(ct);

        Assert.Empty(result);
    }
}
