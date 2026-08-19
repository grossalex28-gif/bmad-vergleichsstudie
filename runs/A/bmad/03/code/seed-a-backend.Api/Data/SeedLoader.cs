using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public static class SeedLoader
{
    public static async Task SeedAsync(AppDbContext db, string anfangsdatenbestandPfad)
    {
        if (await db.Spielstaetten.AnyAsync())
        {
            return;
        }

        var json = await File.ReadAllTextAsync(anfangsdatenbestandPfad);
        var bestand = JsonSerializer.Deserialize<AnfangsdatenbestandDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException($"Anfangsdatenbestand konnte nicht gelesen werden: {anfangsdatenbestandPfad}");

        var raeumeNachId = new Dictionary<string, Raum>();

        foreach (var spielstaetteDto in bestand.Spielstaetten)
        {
            var spielstaette = new Spielstaette
            {
                Id = spielstaetteDto.Id,
                Name = spielstaetteDto.Name
            };
            db.Spielstaetten.Add(spielstaette);

            foreach (var raumDto in spielstaetteDto.Raeume)
            {
                var raum = new Raum
                {
                    Id = raumDto.Id,
                    Name = raumDto.Name,
                    SpielstaetteId = spielstaette.Id,
                    Reihen = raumDto.Reihen,
                    Spalten = raumDto.Spalten,
                    GangSpalten = raumDto.Gang?.Spalten ?? []
                };
                db.Raeume.Add(raum);
                raeumeNachId[raum.Id] = raum;
            }
        }

        foreach (var veranstaltungDto in bestand.Veranstaltungen)
        {
            var veranstaltung = new Veranstaltung
            {
                Id = veranstaltungDto.Id,
                Titel = veranstaltungDto.Titel,
                Beschreibung = veranstaltungDto.Beschreibung,
                RaumId = veranstaltungDto.RaumId,
                DauerMinuten = veranstaltungDto.DauerMinuten,
                Altersfreigabe = veranstaltungDto.Altersfreigabe,
                Zeitpunkt = veranstaltungDto.Zeitpunkt
            };
            db.Veranstaltungen.Add(veranstaltung);

            foreach (var preiskategorieDto in veranstaltungDto.Preiskategorien)
            {
                db.Preiskategorien.Add(new Preiskategorie
                {
                    Id = preiskategorieDto.Id,
                    Name = preiskategorieDto.Name,
                    Preis = preiskategorieDto.Preis,
                    VeranstaltungId = veranstaltung.Id
                });
            }

            foreach (var code in veranstaltungDto.BelegteSitzplaetze)
            {
                db.Sitzplatzbelegungen.Add(new Sitzplatzbelegung
                {
                    VeranstaltungId = veranstaltung.Id,
                    SitzplatzCode = code,
                    BuchungId = null
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private sealed class AnfangsdatenbestandDto
    {
        public List<SpielstaetteDto> Spielstaetten { get; set; } = [];

        public List<VeranstaltungDto> Veranstaltungen { get; set; } = [];
    }

    private sealed class SpielstaetteDto
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public List<RaumDto> Raeume { get; set; } = [];
    }

    private sealed class RaumDto
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public string[] Reihen { get; set; } = [];

        public int Spalten { get; set; }

        public GangDto? Gang { get; set; }
    }

    private sealed class GangDto
    {
        public int[] Spalten { get; set; } = [];

        public string? Hinweis { get; set; }
    }

    private sealed class VeranstaltungDto
    {
        public required string Id { get; set; }

        public required string Titel { get; set; }

        public required string Beschreibung { get; set; }

        [JsonPropertyName("spielstaetteId")]
        public string SpielstaetteId { get; set; } = "";

        public required string RaumId { get; set; }

        [JsonConverter(typeof(OffsetlosAlsEuropaBerlinZeitpunktConverter))]
        public required DateTimeOffset Zeitpunkt { get; set; }

        public int DauerMinuten { get; set; }

        public int Altersfreigabe { get; set; }

        public List<PreiskategorieDto> Preiskategorien { get; set; } = [];

        public List<string> BelegteSitzplaetze { get; set; } = [];
    }

    private sealed class PreiskategorieDto
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public decimal Preis { get; set; }
    }
}
