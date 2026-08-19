using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure.SeedModels;

namespace seed_a_backend.Api.Infrastructure;

public class SeedDataImporter(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task ImportIfEmptyAsync(string jsonFilePath, CancellationToken ct = default)
    {
        if (await db.Venues.AnyAsync(ct))
        {
            return;
        }

        if (!File.Exists(jsonFilePath))
        {
            throw new InvalidOperationException($"Anfangsdatenbestand '{jsonFilePath}' wurde nicht gefunden.");
        }

        var json = await File.ReadAllTextAsync(jsonFilePath, ct);
        SeedDataDto? data;
        try
        {
            data = JsonSerializer.Deserialize<SeedDataDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Anfangsdatenbestand '{jsonFilePath}' konnte nicht gelesen werden: {ex.Message}", ex);
        }

        if (data is null)
        {
            throw new InvalidOperationException($"Anfangsdatenbestand konnte nicht aus '{jsonFilePath}' gelesen werden.");
        }

        await ImportAsync(data, ct);
    }

    public async Task ImportAsync(SeedDataDto data, CancellationToken ct = default)
    {
        var spielstaetten = data.Spielstaetten
            ?? throw new InvalidOperationException("Anfangsdatenbestand enthält kein 'spielstaetten'-Feld.");
        var veranstaltungen = data.Veranstaltungen
            ?? throw new InvalidOperationException("Anfangsdatenbestand enthält kein 'veranstaltungen'-Feld.");

        var venueIds = new HashSet<string>();
        foreach (var venueDto in spielstaetten)
        {
            var venueId = RequireNonNull(venueDto.Id, "Anfangsdatenbestand enthält eine Spielstätte mit fehlender (null) Id.");
            if (!venueIds.Add(venueId))
            {
                throw new InvalidOperationException($"Spielstätte '{venueId}' ist im Anfangsdatenbestand mehrfach vorhanden.");
            }
        }

        foreach (var eventDto in veranstaltungen)
        {
            var spielstaetteId = RequireNonNull(eventDto.SpielstaetteId, $"Veranstaltung '{eventDto.Id}' hat eine fehlende (null) Spielstätten-Id.");
            if (!venueIds.Contains(spielstaetteId))
            {
                throw new InvalidOperationException(
                    $"Veranstaltung '{eventDto.Id}' referenziert unbekannte Spielstätte '{spielstaetteId}'.");
            }
        }

        var venues = new List<Venue>();

        foreach (var venueDto in spielstaetten)
        {
            var venueId = RequireNonNull(venueDto.Id, "Anfangsdatenbestand enthält eine Spielstätte mit fehlender (null) Id.");
            var venueName = RequireNonNull(venueDto.Name, $"Spielstätte '{venueId}' hat einen fehlenden (null) Namen.");
            var venue = new Venue { Name = venueName };
            var roomsById = new Dictionary<string, Room>();

            var raeume = venueDto.Raeume
                ?? throw new InvalidOperationException($"Spielstätte '{venueId}' enthält kein 'raeume'-Feld.");

            foreach (var roomDto in raeume)
            {
                var roomId = RequireNonNull(roomDto.Id, $"Spielstätte '{venueId}' enthält einen Raum mit fehlender (null) Id.");
                var roomName = RequireNonNull(roomDto.Name, $"Raum '{roomId}' in Spielstätte '{venueId}' hat einen fehlenden (null) Namen.");

                if (roomDto.Spalten < 1)
                {
                    throw new InvalidOperationException(
                        $"Raum '{roomId}' in Spielstätte '{venueId}' hat eine ungültige Spaltenzahl ({roomDto.Spalten}); erwartet wird mindestens 1.");
                }

                var aisleColumns = roomDto.Gang?.Spalten ?? new List<int>();
                foreach (var column in aisleColumns)
                {
                    if (column < 1 || column > roomDto.Spalten)
                    {
                        throw new InvalidOperationException(
                            $"Raum '{roomId}': Gang-Spalte '{column}' liegt außerhalb des Sitzplans (1..{roomDto.Spalten}).");
                    }
                }

                var reihen = roomDto.Reihen
                    ?? throw new InvalidOperationException($"Raum '{roomId}' in Spielstätte '{venueId}' enthält kein 'reihen'-Feld.");
                var rowLabels = reihen
                    .Select(rowLabel => RequireNonNull(
                        rowLabel, $"Raum '{roomId}' in Spielstätte '{venueId}' enthält einen leeren (null) Eintrag in 'reihen'."))
                    .ToList();

                if (!roomsById.TryAdd(roomId, new Room
                {
                    Name = roomName,
                    RowLabels = rowLabels,
                    ColumnCount = roomDto.Spalten,
                    AisleColumns = aisleColumns,
                }))
                {
                    throw new InvalidOperationException(
                        $"Spielstätte '{venueId}' enthält den Raum '{roomId}' mehrfach.");
                }

                venue.Rooms.Add(roomsById[roomId]);
            }

            foreach (var eventDto in veranstaltungen.Where(e => e.SpielstaetteId == venueId))
            {
                var raumId = RequireNonNull(eventDto.RaumId, $"Veranstaltung '{eventDto.Id}' hat eine fehlende (null) Raum-Id.");
                if (!roomsById.TryGetValue(raumId, out var room))
                {
                    throw new InvalidOperationException(
                        $"Veranstaltung '{eventDto.Id}' referenziert unbekannten Raum '{raumId}' in Spielstätte '{venueId}'.");
                }

                if (eventDto.DauerMinuten < 1)
                {
                    throw new InvalidOperationException(
                        $"Veranstaltung '{eventDto.Id}' hat eine ungültige Dauer ({eventDto.DauerMinuten} Minuten); erwartet wird mindestens 1.");
                }

                if (eventDto.Altersfreigabe < 0)
                {
                    throw new InvalidOperationException(
                        $"Veranstaltung '{eventDto.Id}' hat eine ungültige Altersfreigabe ({eventDto.Altersfreigabe}); negative Werte sind nicht erlaubt.");
                }

                var @event = new Event
                {
                    Titel = RequireNonNull(eventDto.Titel, $"Veranstaltung '{eventDto.Id}' hat einen fehlenden (null) Titel."),
                    Beschreibung = RequireNonNull(eventDto.Beschreibung, $"Veranstaltung '{eventDto.Id}' hat eine fehlende (null) Beschreibung."),
                    Zeitpunkt = eventDto.Zeitpunkt,
                    DauerMinuten = eventDto.DauerMinuten,
                    Altersfreigabe = eventDto.Altersfreigabe,
                };

                var preiskategorien = eventDto.Preiskategorien
                    ?? throw new InvalidOperationException($"Veranstaltung '{eventDto.Id}' enthält kein 'preiskategorien'-Feld.");

                foreach (var priceCategoryDto in preiskategorien)
                {
                    if (priceCategoryDto.Preis < 0)
                    {
                        throw new InvalidOperationException(
                            $"Veranstaltung '{eventDto.Id}': Preiskategorie '{priceCategoryDto.Id}' hat einen negativen Preis ({priceCategoryDto.Preis}).");
                    }

                    @event.PriceCategories.Add(new PriceCategory
                    {
                        Name = RequireNonNull(
                            priceCategoryDto.Name,
                            $"Veranstaltung '{eventDto.Id}': Preiskategorie '{priceCategoryDto.Id}' hat einen fehlenden (null) Namen."),
                        Preis = priceCategoryDto.Preis,
                    });
                }

                var belegteSitzplaetze = eventDto.BelegteSitzplaetze
                    ?? throw new InvalidOperationException($"Veranstaltung '{eventDto.Id}' enthält kein 'belegteSitzplaetze'-Feld.");

                var gesehen = new HashSet<(string RowLabel, int ColumnNumber)>();
                foreach (var sitzplatz in belegteSitzplaetze)
                {
                    if (sitzplatz is null)
                    {
                        throw new FormatException(
                            $"Veranstaltung '{eventDto.Id}' enthält einen leeren (null) Eintrag in 'belegteSitzplaetze'.");
                    }

                    var (rowLabel, columnNumber) = ParseSitzplatz(sitzplatz);

                    if (!gesehen.Add((rowLabel, columnNumber)))
                    {
                        throw new InvalidOperationException(
                            $"Veranstaltung '{eventDto.Id}' listet den Sitzplatz '{sitzplatz}' mehrfach in 'belegteSitzplaetze'.");
                    }

                    if (!room.RowLabels.Contains(rowLabel))
                    {
                        throw new InvalidOperationException(
                            $"Veranstaltung '{eventDto.Id}': Sitzplatz '{sitzplatz}' referenziert die unbekannte Reihe '{rowLabel}' in Raum '{eventDto.RaumId}'.");
                    }

                    if (columnNumber < 1 || columnNumber > room.ColumnCount)
                    {
                        throw new InvalidOperationException(
                            $"Veranstaltung '{eventDto.Id}': Sitzplatz '{sitzplatz}' liegt außerhalb der Spaltenzahl (1..{room.ColumnCount}) von Raum '{eventDto.RaumId}'.");
                    }

                    if (room.AisleColumns.Contains(columnNumber))
                    {
                        throw new InvalidOperationException(
                            $"Veranstaltung '{eventDto.Id}': Sitzplatz '{sitzplatz}' liegt auf einer Gang-Spalte von Raum '{eventDto.RaumId}'.");
                    }

                    @event.BookingPositions.Add(new BookingPosition
                    {
                        RowLabel = rowLabel,
                        ColumnNumber = columnNumber,
                        BookingId = null,
                        PriceCategoryId = null,
                        CancelledAtUtc = null,
                    });
                }

                room.Events.Add(@event);
            }

            venues.Add(venue);
        }

        db.Venues.AddRange(venues);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// `required`-Felder in den Seed-DTOs erzwingen bei System.Text.Json nur die Anwesenheit des
    /// JSON-Schlüssels, nicht dessen Nicht-Null-Wert — ein expliziter JSON-`null`-Wert bleibt also
    /// trotz `required`-Typisierung zur Laufzeit möglich und würde ohne diese Prüfung zu rohen
    /// Ausnahmen (z. B. ArgumentNullException bei Dictionary-Keys) statt einer beschreibenden
    /// Fehlermeldung führen.
    /// </summary>
    private static string RequireNonNull(string? value, string errorMessage) =>
        value ?? throw new InvalidOperationException(errorMessage);

    /// <summary>Führende Buchstaben = RowLabel, folgende Ziffern = ColumnNumber (z. B. "B3" -> ("B", 3)).</summary>
    internal static (string RowLabel, int ColumnNumber) ParseSitzplatz(string sitzplatz)
    {
        var digitsStart = 0;
        while (digitsStart < sitzplatz.Length && char.IsAsciiLetter(sitzplatz[digitsStart]))
        {
            digitsStart++;
        }

        var digitsEnd = digitsStart;
        while (digitsEnd < sitzplatz.Length && char.IsAsciiDigit(sitzplatz[digitsEnd]))
        {
            digitsEnd++;
        }

        if (digitsStart == 0 || digitsStart == sitzplatz.Length || digitsEnd != sitzplatz.Length
            || !int.TryParse(sitzplatz[digitsStart..], out var columnNumber))
        {
            throw new FormatException($"Sitzplatz '{sitzplatz}' entspricht nicht dem Format <Reihenbuchstaben><Spaltenzahl>.");
        }

        var rowLabel = sitzplatz[..digitsStart];
        return (rowLabel, columnNumber);
    }
}
