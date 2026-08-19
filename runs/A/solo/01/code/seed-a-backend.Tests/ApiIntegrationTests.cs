using System.Net;
using System.Net.Http.Json;
using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetSpielstaetten_GibtDenAnfangsdatenbestandZurueck()
    {
        var spielstaetten = await _client.GetFromJsonAsync<List<SpielstaetteDto>>(
            "/api/spielstaetten", TestContext.Current.CancellationToken);

        Assert.NotNull(spielstaetten);
        Assert.Contains(spielstaetten, s => s.Name == "Stadthalle Nordpark");
    }

    [Fact]
    public async Task GetVeranstaltungen_MitSpielstaettenFilter_SchraenktEin()
    {
        var alle = await _client.GetFromJsonAsync<List<VeranstaltungListItemDto>>(
            "/api/veranstaltungen", TestContext.Current.CancellationToken);
        Assert.NotNull(alle);
        Assert.True(alle.Count >= 6);

        var gefiltert = await _client.GetFromJsonAsync<List<VeranstaltungListItemDto>>(
            "/api/veranstaltungen?spielstaetteId=V1", TestContext.Current.CancellationToken);

        Assert.NotNull(gefiltert);
        Assert.NotEmpty(gefiltert);
        Assert.All(gefiltert, v => Assert.Equal("V1", v.SpielstaetteId));
    }

    [Fact]
    public async Task GetVeranstaltung_UnbekannteId_Gibt404Zurueck()
    {
        var antwort = await _client.GetAsync("/api/veranstaltungen/UNBEKANNT", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, antwort.StatusCode);
    }

    [Fact]
    public async Task GetSitzplan_ZeigtVorbelegteSitzplaetzeAusDemAnfangsdatenbestand()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>(
            "/api/veranstaltungen/E1/sitzplan", TestContext.Current.CancellationToken);

        Assert.NotNull(sitzplan);
        Assert.Contains(sitzplan.BelegtePlaetze, p => p.Reihe == "B" && p.Spalte == 3);
    }

    [Fact]
    public async Task PostBuchung_ErstelltUndKannAnschliessendAbgerufenWerden()
    {
        var anfrage = new BuchungCreateRequestDto(
            "E2", "Erika Mustermann", "erika@example.com",
            [new BuchungPositionRequestDto("B", 5, "E2-A")]);

        var erstellenAntwort = await _client.PostAsJsonAsync(
            "/api/buchungen", anfrage, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, erstellenAntwort.StatusCode);

        var buchung = await erstellenAntwort.Content.ReadFromJsonAsync<BuchungResponseDto>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(buchung);
        Assert.Equal(45m, buchung.Gesamtpreis);

        var abrufAntwort = await _client.GetAsync(
            $"/api/buchungen/{buchung.Referenz}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, abrufAntwort.StatusCode);

        var stornierenAntwort = await _client.PostAsync(
            $"/api/buchungen/{buchung.Referenz}/stornieren", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, stornierenAntwort.StatusCode);
    }

    [Fact]
    public async Task PostBuchung_UnbekannteVeranstaltung_Gibt404Zurueck()
    {
        var anfrage = new BuchungCreateRequestDto(
            "UNBEKANNT", "Erika Mustermann", "erika@example.com",
            [new BuchungPositionRequestDto("B", 5, "X")]);

        var antwort = await _client.PostAsJsonAsync("/api/buchungen", anfrage, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, antwort.StatusCode);
    }

    [Fact]
    public async Task GetBuchung_UnbekannteReferenz_Gibt404Zurueck()
    {
        var antwort = await _client.GetAsync("/api/buchungen/UNBEKANNT1", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, antwort.StatusCode);
    }
}
