using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Controllers.Dtos;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

public class VenuesControllerTests : IAsyncLifetime
{
    private readonly string _connectionString = SqlServerTestDatabase.CreateIsolatedConnectionString();

    private static readonly string SeedFixturePath = Path.Combine(AppContext.BaseDirectory, "TestData", "seed-fixture.json");

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task GetVenues_liefert_200_mit_den_Spielstaetten_aus_dem_Datenbestand()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/venues", ct);
        response.EnsureSuccessStatusCode();

        var venues = await response.Content.ReadFromJsonAsync<List<VenueDto>>(ct);

        Assert.NotNull(venues);
        Assert.Equal(2, venues!.Count);
        Assert.Collection(venues,
            v =>
            {
                Assert.Equal(1, v.Id);
                Assert.Equal("Test-Spielstaette Nord", v.Name);
            },
            v =>
            {
                Assert.Equal(2, v.Id);
                Assert.Equal("Test-Spielstaette Sued", v.Name);
            });
    }

    [Fact]
    public async Task GetVenues_gegen_leere_Datenbank_liefert_200_mit_leerem_Array()
    {
        var ct = TestContext.Current.CancellationToken;

        var leereSeedDatei = Path.Combine(Path.GetTempPath(), $"seed-fixture-leer-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(leereSeedDatei, """{ "spielstaetten": [], "veranstaltungen": [] }""", ct);

            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(webBuilder => webBuilder
                    .UseSetting("ConnectionStrings:Default", _connectionString)
                    .UseSetting("Seed:FilePath", leereSeedDatei));

            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/venues", ct);
            response.EnsureSuccessStatusCode();

            var venues = await response.Content.ReadFromJsonAsync<List<VenueDto>>(ct);

            Assert.NotNull(venues);
            Assert.Empty(venues!);
        }
        finally
        {
            File.Delete(leereSeedDatei);
        }
    }
}
