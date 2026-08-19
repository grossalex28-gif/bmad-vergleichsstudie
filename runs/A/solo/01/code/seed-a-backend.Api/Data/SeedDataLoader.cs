using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Data;

public static partial class SeedDataLoader
{
    public static async Task LoadIfEmptyAsync(AppDbContext db, string dateiPfad, CancellationToken ct = default)
    {
        if (await db.Spielstaetten.AnyAsync(ct))
        {
            return;
        }

        if (!File.Exists(dateiPfad))
        {
            throw new FileNotFoundException($"Anfangsdatenbestand nicht gefunden: {dateiPfad}");
        }

        var json = await File.ReadAllTextAsync(dateiPfad, ct);
        var daten = JsonSerializer.Deserialize<AnfangsdatenbestandDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        foreach (var spielstaetteDto in daten.Spielstaetten)
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
                    SpielstaetteId = spielstaette.Id,
                    ReihenCsv = string.Join(',', raumDto.Reihen),
                    Spalten = raumDto.Spalten,
                    GangSpaltenCsv = raumDto.Gang is { Spalten.Count: > 0 }
                        ? string.Join(',', raumDto.Gang.Spalten)
                        : null,
                    GangHinweis = raumDto.Gang?.Hinweis
                });
            }

            db.Spielstaetten.Add(spielstaette);
        }

        foreach (var veranstaltungDto in daten.Veranstaltungen)
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

            foreach (var preisDto in veranstaltungDto.Preiskategorien)
            {
                veranstaltung.Preiskategorien.Add(new Preiskategorie
                {
                    Id = preisDto.Id,
                    VeranstaltungId = veranstaltung.Id,
                    Name = preisDto.Name,
                    Preis = preisDto.Preis
                });
            }

            db.Veranstaltungen.Add(veranstaltung);

            if (veranstaltungDto.BelegteSitzplaetze.Count > 0)
            {
                var ersteKategorieId = veranstaltungDto.Preiskategorien.First().Id;

                var vorbelegung = new Buchung
                {
                    Id = Guid.NewGuid(),
                    Referenz = ReferenzGenerator.NeueReferenz(),
                    Name = "Vorbelegung",
                    Email = "vorbelegung@system.local",
                    ErstelltAm = DateTime.UtcNow,
                    Status = BuchungStatus.Aktiv,
                    VeranstaltungId = veranstaltung.Id
                };

                foreach (var sitzplatz in veranstaltungDto.BelegteSitzplaetze)
                {
                    var (reihe, spalte) = SitzplatzBezeichnungParsen(sitzplatz);
                    var preis = veranstaltungDto.Preiskategorien.First(p => p.Id == ersteKategorieId).Preis;

                    vorbelegung.Positionen.Add(new Buchungsposition
                    {
                        Id = Guid.NewGuid(),
                        BuchungId = vorbelegung.Id,
                        VeranstaltungId = veranstaltung.Id,
                        Reihe = reihe,
                        Spalte = spalte,
                        PreiskategorieId = ersteKategorieId,
                        Preis = preis,
                        Status = BuchungStatus.Aktiv
                    });
                }

                db.Buchungen.Add(vorbelegung);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static (string reihe, int spalte) SitzplatzBezeichnungParsen(string bezeichnung)
    {
        var match = SitzplatzRegex().Match(bezeichnung);
        if (!match.Success)
        {
            throw new FormatException($"Ungültige Sitzplatzbezeichnung im Anfangsdatenbestand: {bezeichnung}");
        }

        return (match.Groups["reihe"].Value, int.Parse(match.Groups["spalte"].Value));
    }

    [GeneratedRegex(@"^(?<reihe>[A-Za-z]+)(?<spalte>\d+)$")]
    private static partial Regex SitzplatzRegex();
}
