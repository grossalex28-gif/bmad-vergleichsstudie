using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class VeranstaltungenEndpointTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var connectionString = SqlServerTestDatabase.BuildConnectionString(nameof(VeranstaltungenEndpointTests));

        // Datenbank leer + migriert vorbereiten, damit Program.cs beim Hoststart den Anfangsdatenbestand seedet
        var db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(VeranstaltungenEndpointTests));
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
        await SqlServerTestDatabase.DropAsync(nameof(VeranstaltungenEndpointTests));
    }

    [Fact]
    public async Task GetVeranstaltungen_LiefertAlleVeranstaltungenAufsteigendNachZeitpunktSortiert()
    {
        var veranstaltungen = await _client.GetFromJsonAsync<List<VeranstaltungListeDto>>("/veranstaltungen");

        Assert.NotNull(veranstaltungen);
        Assert.Equal(6, veranstaltungen!.Count);
        Assert.Equal(veranstaltungen.OrderBy(v => v.Zeitpunkt).Select(v => v.Id), veranstaltungen.Select(v => v.Id));
        Assert.All(veranstaltungen, v =>
        {
            Assert.False(string.IsNullOrWhiteSpace(v.Titel));
            Assert.False(string.IsNullOrWhiteSpace(v.SpielstaetteName));
        });
    }

    [Fact]
    public async Task GetVeranstaltungen_OffsetloserZeitpunktImAnfangsdatenbestand_WirdAlsEuropaBerlinOffsetAusgeliefert()
    {
        var veranstaltungen = await _client.GetFromJsonAsync<List<VeranstaltungListeDto>>("/veranstaltungen");

        var e1 = veranstaltungen!.Single(v => v.Id == "E1");

        Assert.Equal(TimeSpan.FromHours(2), e1.Zeitpunkt.Offset);
    }

    [Fact]
    public async Task GetVeranstaltungen_MitVonUndBisQueryParametern_FiltertAufDenAnfangsdatenbestand()
    {
        var veranstaltungen = await _client.GetFromJsonAsync<List<VeranstaltungListeDto>>(
            "/veranstaltungen?von=2026-09-01&bis=2026-09-30");

        Assert.NotNull(veranstaltungen);
        Assert.Equal(["E1", "E2", "E3", "E4"], veranstaltungen!.Select(v => v.Id));
    }

    [Fact]
    public async Task GetVeranstaltungen_MitSpielstaetteIdQueryParameter_FiltertAufDenAnfangsdatenbestand()
    {
        var veranstaltungen = await _client.GetFromJsonAsync<List<VeranstaltungListeDto>>(
            "/veranstaltungen?spielstaetteId=V2");

        Assert.NotNull(veranstaltungen);
        Assert.Equal(["E2", "E4", "E6"], veranstaltungen!.Select(v => v.Id));
        Assert.All(veranstaltungen, v => Assert.Equal("V2", v.SpielstaetteId));
    }

    [Fact]
    public async Task GetVeranstaltung_ExistierendeId_LiefertDetailsAusAnfangsdatenbestand()
    {
        var veranstaltung = await _client.GetFromJsonAsync<VeranstaltungDetailDto>("/veranstaltungen/E1");

        Assert.NotNull(veranstaltung);
        Assert.Equal("Kammerkonzert Frühling", veranstaltung!.Titel);
        Assert.Equal("Ein intimes Konzert mit klassischen und modernen Stücken für ein kleines Ensemble.", veranstaltung.Beschreibung);
        Assert.Equal(90, veranstaltung.DauerMinuten);
        Assert.Equal(0, veranstaltung.Altersfreigabe);
        Assert.Equal("Stadthalle Nordpark", veranstaltung.SpielstaetteName);
        Assert.Equal("Kleiner Saal", veranstaltung.RaumName);
        Assert.Equal(TimeSpan.FromHours(2), veranstaltung.Zeitpunkt.Offset);
        Assert.Equal(2, veranstaltung.Preiskategorien.Count);
        Assert.Equal(["E1-A", "E1-B"], veranstaltung.Preiskategorien.Select(p => p.Id));
        Assert.Equal("Kategorie A", veranstaltung.Preiskategorien[0].Name);
        Assert.Equal(32.00m, veranstaltung.Preiskategorien[0].Preis);
    }

    [Fact]
    public async Task GetVeranstaltung_NichtExistierendeId_Liefert404AlsProblemDetails()
    {
        var antwort = await _client.GetAsync("/veranstaltungen/E-unbekannt");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);

        var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails!.Status);
    }

    [Fact]
    public async Task GetSitzplan_VeranstaltungE1_LiefertB3B4C7AlsBelegtAlleAnderenAlsFrei()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>("/veranstaltungen/E1/sitzplan");

        Assert.NotNull(sitzplan);
        Assert.Equal("R2", sitzplan!.RaumId);

        var sitzplaetze = sitzplan.Reihen.SelectMany(r => r.Positionen).Where(p => p.Typ == "Sitzplatz").ToList();
        Assert.Equal(54, sitzplaetze.Count);

        var belegte = sitzplaetze.Where(p => p.Status == "Belegt").Select(p => p.Code).ToList();
        Assert.Equal(["B3", "B4", "C7"], belegte);

        Assert.All(sitzplan.Reihen, reihe => Assert.Equal("Gang", reihe.Positionen.Single(p => p.Spalte == 6).Typ));
    }

    [Fact]
    public async Task GetSitzplan_VeranstaltungOhneBelegteSitzplaetze_AlleSitzplaetzeSindFrei()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>("/veranstaltungen/E2/sitzplan");

        Assert.NotNull(sitzplan);
        var sitzplaetze = sitzplan!.Reihen.SelectMany(r => r.Positionen).Where(p => p.Typ == "Sitzplatz");
        Assert.All(sitzplaetze, p => Assert.Equal("Frei", p.Status));
    }

    [Fact]
    public async Task GetSitzplan_RaumR1MitAsymmetrischerGeometrie_GangBeiSpalte8()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>("/veranstaltungen/E3/sitzplan");

        Assert.NotNull(sitzplan);
        Assert.Equal(10, sitzplan!.Reihen.Count);
        Assert.All(sitzplan.Reihen, reihe =>
        {
            Assert.Equal(14, reihe.Positionen.Count);
            Assert.Equal(["Gang"], reihe.Positionen.Where(p => p.Typ == "Gang").Select(p => p.Typ));
            Assert.Equal(8, reihe.Positionen.Single(p => p.Typ == "Gang").Spalte);
        });
    }

    [Fact]
    public async Task GetSitzplan_RaumR3MitBeidseitigenSeitengaengen_GangBeiSpalte1Und16()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>("/veranstaltungen/E2/sitzplan");

        Assert.NotNull(sitzplan);
        Assert.Equal(12, sitzplan!.Reihen.Count);
        Assert.All(sitzplan.Reihen, reihe =>
        {
            Assert.Equal(16, reihe.Positionen.Count);
            var gangSpalten = reihe.Positionen.Where(p => p.Typ == "Gang").Select(p => p.Spalte);
            Assert.Equal([1, 16], gangSpalten);
        });
    }

    [Fact]
    public async Task GetSitzplan_RaumR4OhneGang_AlleSpaltenSindSitzplaetze()
    {
        var sitzplan = await _client.GetFromJsonAsync<SitzplanDto>("/veranstaltungen/E4/sitzplan");

        Assert.NotNull(sitzplan);
        Assert.Equal(4, sitzplan!.Reihen.Count);
        Assert.All(sitzplan.Reihen, reihe =>
        {
            Assert.Equal(8, reihe.Positionen.Count);
            Assert.All(reihe.Positionen, p => Assert.Equal("Sitzplatz", p.Typ));
        });
    }

    [Fact]
    public async Task GetSitzplan_NichtExistierendeVeranstaltung_Liefert404AlsProblemDetails()
    {
        var antwort = await _client.GetAsync("/veranstaltungen/E-unbekannt/sitzplan");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, antwort.StatusCode);
        Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);

        var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails!.Status);
    }
}
