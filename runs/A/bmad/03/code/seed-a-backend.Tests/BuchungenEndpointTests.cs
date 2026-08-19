using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class BuchungenEndpointTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var connectionString = SqlServerTestDatabase.BuildConnectionString(nameof(BuchungenEndpointTests));

        // Datenbank leer + migriert vorbereiten, damit Program.cs beim Hoststart den Anfangsdatenbestand seedet
        var db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(BuchungenEndpointTests));
        await db.DisposeAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", connectionString);
        });
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await SqlServerTestDatabase.DropAsync(nameof(BuchungenEndpointTests));
    }

    [Fact]
    public async Task PostBuchung_VeranstaltungE2MitFreienPlaetzen_Liefert201MitBuchungsdaten()
    {
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("A1", "E2-A"),
                new BuchungspositionRequestDto("A2", "E2-B"),
            ]);

        var antwort = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, antwort.StatusCode);
        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungDto>();
        Assert.NotNull(buchung);
        Assert.False(string.IsNullOrWhiteSpace(buchung!.Referenz));
        Assert.Equal("Aktiv", buchung.Status);
        Assert.True(buchung.Gesamtpreis > 0);
        Assert.Equal($"/buchungen/{buchung.Referenz}", antwort.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostBuchung_VeranstaltungE1MitBereitsBelegtemPlatz_Liefert409MitBetroffenenSitzplaetzen()
    {
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("B3", "E1-A"),
                new BuchungspositionRequestDto("C1", "E1-B"),
            ]);

        var antwort = await _client.PostAsJsonAsync("/veranstaltungen/E1/buchungen", request);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);

        var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("sitzplatz_belegt", problemDetails!.Type);
        var betroffeneSitzplaetze = ((System.Text.Json.JsonElement)problemDetails.Extensions["betroffeneSitzplaetze"]!)
            .EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(["B3"], betroffeneSitzplaetze);
    }

    [Fact]
    public async Task PostBuchung_PreiskategorieAusAndererVeranstaltung_Liefert400AlsProblemDetails()
    {
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("A1", "E2-A")]);

        var antwort = await _client.PostAsJsonAsync("/veranstaltungen/E1/buchungen", request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostBuchung_NichtExistierendeVeranstaltung_Liefert404AlsProblemDetails()
    {
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("A1", "E1-A")]);

        var antwort = await _client.PostAsJsonAsync("/veranstaltungen/E-unbekannt/buchungen", request);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostBuchung_SitzplatzCodeMehrfachInDerselbenAnfrage_Liefert400AlsProblemDetails()
    {
        var request = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [
                new BuchungspositionRequestDto("A1", "E2-A"),
                new BuchungspositionRequestDto("A1", "E2-B"),
            ]);

        var antwort = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
        var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("sitzplatz_duplikat", problemDetails!.Type);
    }

    [Fact]
    public async Task PostBuchung_LeererNameOderKeinePositionen_Liefert400BadRequest()
    {
        var ohneName = new BuchungAnlegenRequestDto(
            "",
            "max@example.com",
            [new BuchungspositionRequestDto("A1", "E2-A")]);
        var ohnePositionen = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            []);

        var antwortOhneName = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", ohneName);
        var antwortOhnePositionen = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", ohnePositionen);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, antwortOhneName.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, antwortOhnePositionen.StatusCode);
    }

    [Fact]
    public async Task GetBuchung_ExistierendeReferenzNachAbschluss_Liefert200MitBuchungsdetails()
    {
        var anlegenRequest = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("B1", "E2-A")]);
        var anlegenAntwort = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", anlegenRequest);
        var angelegteBuchung = await anlegenAntwort.Content.ReadFromJsonAsync<BuchungDto>();

        var antwort = await _client.GetAsync($"/buchungen/{angelegteBuchung!.Referenz}");

        Assert.Equal(System.Net.HttpStatusCode.OK, antwort.StatusCode);
        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungDetailDto>();
        Assert.NotNull(buchung);
        Assert.Equal(angelegteBuchung.Referenz, buchung!.Referenz);
        Assert.Equal("Aktiv", buchung.Status);
        Assert.Single(buchung.Positionen);
    }

    [Fact]
    public async Task GetBuchung_UnbekannteReferenz_Liefert404AlsProblemDetails()
    {
        var antwort = await _client.GetAsync("/buchungen/ZZZZZZZZ");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostStornierung_AktiveBuchung_Liefert200MitStatusStorniertUndSitzplanZeigtPlatzWiederFrei()
    {
        var anlegenRequest = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("C2", "E2-A")]);
        var anlegenAntwort = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", anlegenRequest);
        var angelegteBuchung = await anlegenAntwort.Content.ReadFromJsonAsync<BuchungDto>();

        var antwort = await _client.PostAsJsonAsync($"/buchungen/{angelegteBuchung!.Referenz}/stornierung", new { });

        Assert.Equal(System.Net.HttpStatusCode.OK, antwort.StatusCode);
        var buchung = await antwort.Content.ReadFromJsonAsync<BuchungDetailDto>();
        Assert.NotNull(buchung);
        Assert.Equal("Storniert", buchung!.Status);

        var sitzplanAntwort = await _client.GetAsync("/veranstaltungen/E2/sitzplan");
        var sitzplan = await sitzplanAntwort.Content.ReadFromJsonAsync<SitzplanDto>();
        var position = sitzplan!.Reihen
            .SelectMany(r => r.Positionen)
            .Single(p => p.Code == "C2");
        Assert.Equal("Frei", position.Status);
    }

    [Fact]
    public async Task PostStornierung_BereitsStornierteBuchung_Liefert409MitTypBereitsStorniert()
    {
        var anlegenRequest = new BuchungAnlegenRequestDto(
            "Max Mustermann",
            "max@example.com",
            [new BuchungspositionRequestDto("D2", "E2-A")]);
        var anlegenAntwort = await _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", anlegenRequest);
        var angelegteBuchung = await anlegenAntwort.Content.ReadFromJsonAsync<BuchungDto>();

        await _client.PostAsJsonAsync($"/buchungen/{angelegteBuchung!.Referenz}/stornierung", new { });
        var antwort = await _client.PostAsJsonAsync($"/buchungen/{angelegteBuchung.Referenz}/stornierung", new { });

        Assert.Equal(System.Net.HttpStatusCode.Conflict, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
        var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("bereits_storniert", problemDetails!.Type);
    }

    [Fact]
    public async Task PostStornierung_UnbekannteReferenz_Liefert404AlsProblemDetails()
    {
        var antwort = await _client.PostAsJsonAsync("/buchungen/ZZZZZZZZ/stornierung", new { });

        Assert.Equal(System.Net.HttpStatusCode.NotFound, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
    }
}
