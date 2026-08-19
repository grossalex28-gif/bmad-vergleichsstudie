using seed_a_backend.Api.Data;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Tests;

public record TestVeranstaltung(
    string SpielstaetteId,
    string RaumId,
    string VeranstaltungId,
    string KategorieAId,
    string KategorieBId);

public static class TestDataFactory
{
    /// <summary>
    /// Legt eine Testveranstaltung mit eindeutigen IDs an: Raum mit Reihen A-C, 5 Spalten,
    /// Gang in Spalte 3, sowie zwei Preiskategorien (20 und 10).
    /// </summary>
    public static async Task<TestVeranstaltung> SeedVeranstaltungAsync(
        AppDbContext context,
        DateTime? zeitpunkt = null,
        string? spielstaetteId = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var neueSpielstaette = spielstaetteId is null;
        spielstaetteId ??= $"TEST-V-{suffix}";
        var raumId = $"TEST-R-{suffix}";
        var veranstaltungId = $"TEST-E-{suffix}";
        var kategorieAId = $"{veranstaltungId}-A";
        var kategorieBId = $"{veranstaltungId}-B";

        if (neueSpielstaette)
        {
            context.Spielstaetten.Add(new Spielstaette { Id = spielstaetteId, Name = $"Testspielstätte {suffix}" });
        }

        context.Raeume.Add(new Raum
        {
            Id = raumId,
            Name = $"Testraum {suffix}",
            SpielstaetteId = spielstaetteId,
            Reihen = ["A", "B", "C"],
            Spalten = 5,
            GangSpalten = [3]
        });

        var veranstaltung = new Veranstaltung
        {
            Id = veranstaltungId,
            Titel = $"Testveranstaltung {suffix}",
            Beschreibung = "Beschreibung",
            SpielstaetteId = spielstaetteId,
            RaumId = raumId,
            Zeitpunkt = zeitpunkt ?? DateTime.UtcNow.AddDays(30),
            DauerMinuten = 90,
            Altersfreigabe = 0
        };
        veranstaltung.Preiskategorien.Add(new Preiskategorie { Id = kategorieAId, Name = "Kategorie A", Preis = 20m, VeranstaltungId = veranstaltungId });
        veranstaltung.Preiskategorien.Add(new Preiskategorie { Id = kategorieBId, Name = "Kategorie B", Preis = 10m, VeranstaltungId = veranstaltungId });
        context.Veranstaltungen.Add(veranstaltung);

        await context.SaveChangesAsync();

        return new TestVeranstaltung(spielstaetteId, raumId, veranstaltungId, kategorieAId, kategorieBId);
    }
}
