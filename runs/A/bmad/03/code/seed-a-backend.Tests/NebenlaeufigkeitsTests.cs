using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Tests.TestSupport;

namespace seed_a_backend.Tests;

public class NebenlaeufigkeitsTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var connectionString = SqlServerTestDatabase.BuildConnectionString(nameof(NebenlaeufigkeitsTests));

        var db = await SqlServerTestDatabase.CreateFreshContextAsync(nameof(NebenlaeufigkeitsTests));
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
        await SqlServerTestDatabase.DropAsync(nameof(NebenlaeufigkeitsTests));
    }

    private AppDbContext OeffnePruefContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(SqlServerTestDatabase.BuildConnectionString(nameof(NebenlaeufigkeitsTests)))
            .Options);

    [Fact]
    public async Task PostBuchung_50GleichzeitigeVersucheAufDenselbenSitzplatz_GenauEinErfolgReduziertAufEineBelegung()
    {
        const string sitzplatzCode = "N9";
        const int anzahlVersuche = 50;

        var aufgaben = Enumerable.Range(0, anzahlVersuche)
            .Select(i => _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", new BuchungAnlegenRequestDto(
                $"Besucher {i}",
                $"besucher{i}@example.com",
                [new BuchungspositionRequestDto(sitzplatzCode, "E2-A")])))
            .ToArray();

        var antworten = await Task.WhenAll(aufgaben);

        var erfolgreiche = antworten.Where(a => a.StatusCode == HttpStatusCode.Created).ToList();
        var abgelehnte = antworten.Where(a => a.StatusCode == HttpStatusCode.Conflict).ToList();

        Assert.Single(erfolgreiche);
        Assert.Equal(anzahlVersuche - 1, abgelehnte.Count);
        Assert.Equal(anzahlVersuche, erfolgreiche.Count + abgelehnte.Count); // keine unerwarteten Statuscodes (z. B. 500)

        foreach (var antwort in abgelehnte)
        {
            Assert.Equal("application/problem+json", antwort.Content.Headers.ContentType?.MediaType);
            var problemDetails = await antwort.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal("sitzplatz_belegt", problemDetails!.Type);
        }

        await using var pruefContext = OeffnePruefContext();
        var belegungen = await pruefContext.Sitzplatzbelegungen
            .Where(s => s.VeranstaltungId == "E2" && s.SitzplatzCode == sitzplatzCode)
            .ToListAsync();
        Assert.Single(belegungen);
        Assert.Equal(1, await pruefContext.Buchungen.CountAsync());
    }

    [Fact]
    public async Task PostBuchung_UeberlappendeAuswahlGleichzeitig_NurEineTransaktionVollstaendigAngelegtKeineTeilbuchung()
    {
        var buchungATask = _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", new BuchungAnlegenRequestDto(
            "Besucher A",
            "besucherA@example.com",
            [
                new BuchungspositionRequestDto("P1", "E2-A"),
                new BuchungspositionRequestDto("P2", "E2-A"),
            ]));
        var buchungBTask = _client.PostAsJsonAsync("/veranstaltungen/E2/buchungen", new BuchungAnlegenRequestDto(
            "Besucher B",
            "besucherB@example.com",
            [
                new BuchungspositionRequestDto("P2", "E2-B"),
                new BuchungspositionRequestDto("P3", "E2-B"),
            ]));

        await Task.WhenAll(buchungATask, buchungBTask);
        var antwortA = await buchungATask;
        var antwortB = await buchungBTask;

        // Welche der beiden Transaktionen zuerst committet, ist nicht deterministisch — es MUSS aber
        // genau eine gewinnen (201) und genau eine verlieren (409), niemals beide oder keine.
        var (gewinnerAntwort, verliererAntwort, gewinnerExklusivCode, verliererExklusivCode) =
            antwortA.StatusCode == HttpStatusCode.Created
                ? (antwortA, antwortB, "P1", "P3")
                : (antwortB, antwortA, "P3", "P1");

        Assert.Equal(HttpStatusCode.Created, gewinnerAntwort.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, verliererAntwort.StatusCode);

        await using var pruefContext = OeffnePruefContext();
        Assert.Equal(1, await pruefContext.Buchungen.CountAsync());
        Assert.True(await pruefContext.Sitzplatzbelegungen.AnyAsync(s => s.VeranstaltungId == "E2" && s.SitzplatzCode == "P2"));
        Assert.True(await pruefContext.Sitzplatzbelegungen.AnyAsync(s => s.VeranstaltungId == "E2" && s.SitzplatzCode == gewinnerExklusivCode));
        Assert.False(await pruefContext.Sitzplatzbelegungen.AnyAsync(s => s.VeranstaltungId == "E2" && s.SitzplatzCode == verliererExklusivCode));
    }
}
