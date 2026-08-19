using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Controllers.Dtos;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

public class EventsControllerTests : IAsyncLifetime
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
    public async Task GetEvents_liefert_200_mit_der_erwarteten_JSON_Struktur()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events", ct);
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);

        Assert.NotNull(events);
        Assert.Equal(2, events!.Count);
        Assert.Collection(events,
            e =>
            {
                Assert.Equal(1, e.Id);
                Assert.Equal("Kammerkonzert Fruehling", e.Titel);
                Assert.Equal("Test-Spielstaette Nord", e.Spielstaette);
                Assert.Equal(new DateTime(2026, 9, 5, 19, 30, 0), e.Zeitpunkt);
            },
            e =>
            {
                Assert.Equal(2, e.Id);
                Assert.Equal("Sinfonisches Openair-Programm", e.Titel);
                Assert.Equal("Test-Spielstaette Sued", e.Spielstaette);
                Assert.Equal(new DateTime(2026, 9, 12, 20, 0, 0), e.Zeitpunkt);
            });
    }

    [Fact]
    public async Task GetEvents_mit_Datumsbereich_liefert_nur_die_im_Bereich_liegende_Veranstaltung()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events?von=2026-09-01&bis=2026-09-06", ct);
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);

        Assert.NotNull(events);
        Assert.Single(events!);
        Assert.Equal("Kammerkonzert Fruehling", events![0].Titel);
    }

    [Fact]
    public async Task GetEvents_mit_venueId_liefert_nur_Veranstaltungen_dieser_Spielstaette()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events?venueId=1", ct);
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);

        Assert.NotNull(events);
        Assert.Single(events!);
        Assert.Equal("Kammerkonzert Fruehling", events![0].Titel);
    }

    [Fact]
    public async Task GetEvents_mit_venueId_und_Datumsbereich_kombiniert_beide_Filter_UND_verknuepft()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var treffer = await client.GetAsync("/api/events?venueId=1&von=2026-09-01&bis=2026-09-06", ct);
        treffer.EnsureSuccessStatusCode();
        var trefferEvents = await treffer.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);
        Assert.NotNull(trefferEvents);
        Assert.Single(trefferEvents!);
        Assert.Equal("Kammerkonzert Fruehling", trefferEvents![0].Titel);

        var keinTreffer = await client.GetAsync("/api/events?venueId=2&von=2026-09-01&bis=2026-09-06", ct);
        keinTreffer.EnsureSuccessStatusCode();
        var keinTrefferEvents = await keinTreffer.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);
        Assert.NotNull(keinTrefferEvents);
        Assert.Empty(keinTrefferEvents!);
    }

    [Fact]
    public async Task GetEvent_liefert_200_mit_allen_Feldern_der_Veranstaltung()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events/1", ct);
        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<EventDetailDto>(ct);

        Assert.NotNull(detail);
        Assert.Equal(1, detail!.Id);
        Assert.Equal("Kammerkonzert Fruehling", detail.Titel);
        Assert.Equal(
            "Ein intimes Konzert mit klassischen und modernen Stuecken fuer ein kleines Ensemble.",
            detail.Beschreibung);
        Assert.Equal(90, detail.DauerMinuten);
        Assert.Equal(0, detail.Altersfreigabe);
        Assert.Equal("Test-Spielstaette Nord", detail.Spielstaette);
        Assert.Equal("Kleiner Saal", detail.Raum);
        Assert.Equal(new DateTime(2026, 9, 5, 19, 30, 0), detail.Zeitpunkt);
        Assert.Collection(detail.Preiskategorien,
            pc => { Assert.Equal("Kategorie A", pc.Name); Assert.Equal(32.00m, pc.Preis); },
            pc => { Assert.Equal("Kategorie B", pc.Name); Assert.Equal(22.00m, pc.Preis); });
    }

    [Fact]
    public async Task GetEvent_mit_nicht_existierender_Id_liefert_404_mit_Fehler_Envelope()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999", ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.NotNull(error);
        Assert.Equal("EVENT_NOT_FOUND", error!.Code);
        Assert.False(string.IsNullOrEmpty(error.Message));
    }

    [Fact]
    public async Task GetSeatMap_liefert_200_mit_grid_und_korrektem_Status_je_Zelle()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events/1/seatmap", ct);
        response.EnsureSuccessStatusCode();

        var seatMap = await response.Content.ReadFromJsonAsync<SeatMapDto>(ct);

        Assert.NotNull(seatMap);
        Assert.Equal(["A", "B", "C", "D", "E", "F"], seatMap!.RowLabels);
        Assert.Equal(10, seatMap.ColumnCount);
        Assert.Equal([6], seatMap.AisleColumns);
        Assert.Equal(6, seatMap.Rows.Count);

        var rowB = seatMap.Rows.Single(r => r.RowLabel == "B");
        Assert.Equal("occupied", rowB.Cells.Single(c => c.ColumnNumber == 3).Status);
        Assert.Equal("occupied", rowB.Cells.Single(c => c.ColumnNumber == 4).Status);
        Assert.Equal("free", rowB.Cells.Single(c => c.ColumnNumber == 1).Status);
        Assert.Equal("aisle", rowB.Cells.Single(c => c.ColumnNumber == 6).Status);

        var rowC = seatMap.Rows.Single(r => r.RowLabel == "C");
        Assert.Equal("occupied", rowC.Cells.Single(c => c.ColumnNumber == 7).Status);
    }

    [Fact]
    public async Task GetSeatMap_mit_nicht_existierender_Id_liefert_404_mit_Fehler_Envelope()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999/seatmap", ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.NotNull(error);
        Assert.Equal("EVENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetEvents_gegen_leere_Datenbank_liefert_200_mit_leerem_Array()
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

            var response = await client.GetAsync("/api/events", ct);
            response.EnsureSuccessStatusCode();

            var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>(ct);

            Assert.NotNull(events);
            Assert.Empty(events!);
        }
        finally
        {
            File.Delete(leereSeedDatei);
        }
    }
}
