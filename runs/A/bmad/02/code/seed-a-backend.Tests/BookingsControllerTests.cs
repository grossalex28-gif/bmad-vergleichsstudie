using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Controllers.Dtos;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

public class BookingsControllerTests : IAsyncLifetime
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

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

    private static async Task<int> GetFirstPriceCategoryIdAsync(HttpClient client, CancellationToken ct)
    {
        var detail = await client.GetFromJsonAsync<EventDetailDto>("/api/events/1", ct);
        return detail!.Preiskategorien[0].Id;
    }

    [Fact]
    public async Task CreateBooking_mit_gueltigen_Daten_liefert_201_mit_Referenz_Positionen_und_Gesamtpreis()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);

        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.NotNull(booking);
        Assert.Matches("^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{8}$", booking!.Reference);
        Assert.Equal(32.00m, booking.Gesamtpreis);
        Assert.Equal("aktiv", booking.Status);
        Assert.Single(booking.Positionen);
    }

    [Fact]
    public async Task CreateBooking_berechnet_den_Gesamtpreis_serverseitig_ueber_mehrere_Positionen()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var detail = await client.GetFromJsonAsync<EventDetailDto>("/api/events/1", ct);
        var kategorieA = detail!.Preiskategorien[0];
        var kategorieB = detail.Preiskategorien[1];

        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
        [
            new CreateBookingPositionRequestDto("A", 1, kategorieA.Id),
            new CreateBookingPositionRequestDto("A", 2, kategorieB.Id),
        ]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.NotNull(booking);
        Assert.Equal(kategorieA.Preis + kategorieB.Preis, booking!.Gesamtpreis);
    }

    [Fact]
    public async Task CreateBooking_mit_leerem_Namen_liefert_400_VALIDATION_ERROR()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "  ", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_mit_ungueltiger_Email_liefert_400_VALIDATION_ERROR()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "keine-email",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_ohne_Sitzplaetze_liefert_400_VALIDATION_ERROR()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com", []);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_mit_Sitzplatz_auf_Gang_Spalte_liefert_400_VALIDATION_ERROR()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 6, priceCategoryId)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_mit_unbekannter_PriceCategoryId_liefert_400_VALIDATION_ERROR()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, 999999)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_mit_nicht_existierender_EventId_liefert_404_EVENT_NOT_FOUND()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = new CreateBookingRequestDto(999999, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, 1)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("EVENT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_fuer_bereits_belegten_Sitzplatz_liefert_409_SEAT_CONFLICT_ohne_Teilbuchung()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString);
        await using var dbVorher = new AppDbContext(optionsBuilder.Options);
        var anzahlVorher = await dbVorher.Bookings.CountAsync(ct);

        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("B", 3, priceCategoryId)]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("SEAT_CONFLICT", error!.Code);
        var conflictingSeats = ((JsonElement)error.Details!).Deserialize<List<string>>();
        Assert.Contains("B3", conflictingSeats!);

        await using var dbNachher = new AppDbContext(optionsBuilder.Options);
        var anzahlNachher = await dbNachher.Bookings.CountAsync(ct);
        Assert.Equal(anzahlVorher, anzahlNachher);
    }

    [Fact]
    public async Task CreateBooking_ohne_Request_Body_Feld_liefert_400_mit_AD7_Envelope()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var ohneName = new
        {
            eventId = 1,
            email = "erika@example.com",
            positionen = new[] { new { rowLabel = "A", columnNumber = 1, priceCategoryId } }
        };

        var response = await client.PostAsJsonAsync("/api/bookings", ohneName, ct);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task CreateBooking_fuegt_Sitzplaetze_sortiert_nach_RowLabel_dann_ColumnNumber_ein()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
        [
            new CreateBookingPositionRequestDto("C", 1, priceCategoryId),
            new CreateBookingPositionRequestDto("A", 1, priceCategoryId),
            new CreateBookingPositionRequestDto("B", 1, priceCategoryId),
        ]);

        var response = await client.PostAsJsonAsync("/api/bookings", request, ct);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);
        var gespeicherteBooking = await db.Bookings.SingleAsync(b => b.Reference == booking!.Reference, ct);
        var positionen = await db.BookingPositions
            .Where(bp => bp.BookingId == gespeicherteBooking.Id)
            .OrderBy(bp => bp.Id)
            .Select(bp => bp.RowLabel + bp.ColumnNumber)
            .ToListAsync(ct);

        Assert.Equal(["A1", "B1", "C1"], positionen);
    }

    [Fact]
    public async Task GetBooking_mit_gueltiger_Referenz_liefert_200_mit_Name_Positionen_und_Gesamtpreis()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var response = await client.GetAsync($"/api/bookings/{erstellteBooking!.Reference}", ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.NotNull(booking);
        Assert.Equal("Erika Musterfrau", booking!.Name);
        Assert.Equal(erstellteBooking.Gesamtpreis, booking.Gesamtpreis);
        Assert.Single(booking.Positionen);
        Assert.Equal("aktiv", booking.Status);
    }

    [Fact]
    public async Task GetBooking_mit_Referenz_in_anderer_Gross_Kleinschreibung_liefert_dieselbe_Buchung()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var response = await client.GetAsync($"/api/bookings/{erstellteBooking!.Reference.ToLowerInvariant()}", ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.Equal(erstellteBooking.Reference, booking!.Reference);
    }

    [Fact]
    public async Task GetBooking_mit_nicht_existierender_Referenz_liefert_404_BOOKING_NOT_FOUND()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bookings/ZZZZZZZZ", ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("BOOKING_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetBooking_fuer_stornierte_Buchung_liefert_Status_storniert()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using (var db = new AppDbContext(options))
        {
            var gespeicherteBooking = await db.Bookings.SingleAsync(b => b.Reference == erstellteBooking!.Reference, ct);
            gespeicherteBooking.CancelledAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var response = await client.GetAsync($"/api/bookings/{erstellteBooking!.Reference}", ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.Equal("storniert", booking!.Status);
    }

    [Fact]
    public async Task CancelBooking_mit_gueltiger_Referenz_liefert_200_und_setzt_CancelledAtUtc_auf_Buchung_und_Positionen()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId), new CreateBookingPositionRequestDto("A", 2, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var response = await client.PostAsync($"/api/bookings/{erstellteBooking!.Reference}/cancel", null, ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.Equal("storniert", booking!.Status);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);
        var gespeicherteBooking = await db.Bookings
            .Include(b => b.Positions)
            .SingleAsync(b => b.Reference == erstellteBooking.Reference, ct);
        Assert.NotNull(gespeicherteBooking.CancelledAtUtc);
        Assert.Equal(2, gespeicherteBooking.Positions.Count);
        Assert.All(gespeicherteBooking.Positions, p => Assert.NotNull(p.CancelledAtUtc));
    }

    [Fact]
    public async Task CancelBooking_gibt_stornierte_Sitzplaetze_im_Sitzplan_wieder_frei()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 4, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var seatmapVorStorno = await client.GetFromJsonAsync<SeatMapDto>("/api/events/1/seatmap", ct);
        var zelleVorStorno = seatmapVorStorno!.Rows.Single(r => r.RowLabel == "A").Cells.Single(c => c.ColumnNumber == 4);
        Assert.Equal("occupied", zelleVorStorno.Status);

        var cancelResponse = await client.PostAsync($"/api/bookings/{erstellteBooking!.Reference}/cancel", null, ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, cancelResponse.StatusCode);

        var seatmapNachStorno = await client.GetFromJsonAsync<SeatMapDto>("/api/events/1/seatmap", ct);
        var zelleNachStorno = seatmapNachStorno!.Rows.Single(r => r.RowLabel == "A").Cells.Single(c => c.ColumnNumber == 4);
        Assert.Equal("free", zelleNachStorno.Status);
    }

    [Fact]
    public async Task CancelBooking_mit_Referenz_in_anderer_Gross_Kleinschreibung_storniert_dieselbe_Buchung()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var response = await client.PostAsync($"/api/bookings/{erstellteBooking!.Reference.ToLowerInvariant()}/cancel", null, ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.Equal(erstellteBooking.Reference, booking!.Reference);
        Assert.Equal("storniert", booking.Status);
    }

    [Fact]
    public async Task CancelBooking_mit_nicht_existierender_Referenz_liefert_404_BOOKING_NOT_FOUND()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/bookings/ZZZZZZZZ/cancel", null, ct);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("BOOKING_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task CancelBooking_fuer_bereits_stornierte_Buchung_liefert_409_ALREADY_CANCELLED()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var ersterVersuch = await client.PostAsync($"/api/bookings/{erstellteBooking!.Reference}/cancel", null, ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, ersterVersuch.StatusCode);

        var zweiterVersuch = await client.PostAsync($"/api/bookings/{erstellteBooking.Reference}/cancel", null, ct);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, zweiterVersuch.StatusCode);
        var error = await zweiterVersuch.Content.ReadFromJsonAsync<ErrorEnvelopeDto>(ct);
        Assert.Equal("ALREADY_CANCELLED", error!.Code);
    }

    [Fact]
    public async Task GetBooking_nach_CancelBooking_ueber_denselben_Endpunkt_liefert_Status_storniert()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var priceCategoryId = await GetFirstPriceCategoryIdAsync(client, ct);
        var request = new CreateBookingRequestDto(1, "Erika Musterfrau", "erika@example.com",
            [new CreateBookingPositionRequestDto("A", 1, priceCategoryId)]);
        var createResponse = await client.PostAsJsonAsync("/api/bookings", request, ct);
        var erstellteBooking = await createResponse.Content.ReadFromJsonAsync<BookingDto>(ct);

        var cancelResponse = await client.PostAsync($"/api/bookings/{erstellteBooking!.Reference}/cancel", null, ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, cancelResponse.StatusCode);

        var response = await client.GetAsync($"/api/bookings/{erstellteBooking.Reference}", ct);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(ct);
        Assert.Equal("storniert", booking!.Status);
    }
}
