using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public static partial class SeedDataLoader
{
    public static async Task LoadIfEmptyAsync(AppDbContext context, string jsonFilePath, CancellationToken cancellationToken = default)
    {
        if (await context.Spielstaetten.AnyAsync(cancellationToken))
        {
            return;
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException("Anfangsdatenbestand nicht gefunden.", jsonFilePath);
        }

        var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var data = JsonSerializer.Deserialize<AnfangsdatenbestandDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        foreach (var spielstaetteDto in data.Spielstaetten)
        {
            var spielstaette = new Spielstaette
            {
                Id = spielstaetteDto.Id,
                Name = spielstaetteDto.Name
            };

            foreach (var raumDto in spielstaetteDto.Raeume)
            {
                spielstaette.Raeume.Add(new Raum
                {
                    Id = raumDto.Id,
                    Name = raumDto.Name,
                    SpielstaetteId = spielstaetteDto.Id,
                    Reihen = raumDto.Reihen,
                    Spalten = raumDto.Spalten,
                    GangSpalten = raumDto.Gang?.Spalten ?? new List<int>(),
                    GangHinweis = raumDto.Gang?.Hinweis
                });
            }

            context.Spielstaetten.Add(spielstaette);
        }

        foreach (var veranstaltungDto in data.Veranstaltungen)
        {
            var veranstaltung = new Veranstaltung
            {
                Id = veranstaltungDto.Id,
                Titel = veranstaltungDto.Titel,
                Beschreibung = veranstaltungDto.Beschreibung,
                SpielstaetteId = veranstaltungDto.SpielstaetteId,
                RaumId = veranstaltungDto.RaumId,
                Zeitpunkt = veranstaltungDto.Zeitpunkt,
                DauerMinuten = veranstaltungDto.DauerMinuten,
                Altersfreigabe = veranstaltungDto.Altersfreigabe
            };

            foreach (var pkDto in veranstaltungDto.Preiskategorien)
            {
                veranstaltung.Preiskategorien.Add(new Preiskategorie
                {
                    Id = pkDto.Id,
                    Name = pkDto.Name,
                    Preis = pkDto.Preis,
                    VeranstaltungId = veranstaltungDto.Id
                });
            }

            context.Veranstaltungen.Add(veranstaltung);

            if (veranstaltungDto.BelegteSitzplaetze.Count > 0)
            {
                var erstkategorie = veranstaltungDto.Preiskategorien.First();
                var vorbelegung = new Buchung
                {
                    Referenz = $"SEED-{veranstaltungDto.Id}",
                    Name = "Vorbelegung (Anfangsdatenbestand)",
                    Email = "vorbelegung@example.invalid",
                    VeranstaltungId = veranstaltungDto.Id,
                    Status = BuchungStatus.Bestaetigt
                };

                foreach (var sitzplatz in veranstaltungDto.BelegteSitzplaetze)
                {
                    var (reihe, spalte) = ParseSitzplatz(sitzplatz);
                    vorbelegung.Positionen.Add(new BuchungsPosition
                    {
                        VeranstaltungId = veranstaltungDto.Id,
                        Reihe = reihe,
                        Spalte = spalte,
                        PreiskategorieId = erstkategorie.Id,
                        Preis = erstkategorie.Preis,
                        Aktiv = true
                    });
                }

                context.Buchungen.Add(vorbelegung);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static (string Reihe, int Spalte) ParseSitzplatz(string code)
    {
        var match = SitzplatzPattern().Match(code);
        if (!match.Success)
        {
            throw new FormatException($"Ungültiger Sitzplatzcode: '{code}'.");
        }

        return (match.Groups["reihe"].Value, int.Parse(match.Groups["spalte"].Value));
    }

    [GeneratedRegex(@"^(?<reihe>[A-Za-z]+)(?<spalte>\d+)$")]
    private static partial Regex SitzplatzPattern();
}
