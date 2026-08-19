using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data.Seed;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Api.Data;

public static class SeedImporter
{
    private const string RelativeJsonPath = "Data/anfangsdatenbestand.json";

    public static async Task ImportAsync(AppDbContext dbContext, BookingService bookingService, string contentRootPath)
    {
        if (await dbContext.Venues.AnyAsync())
        {
            return;
        }

        var jsonPath = Path.Combine(contentRootPath, RelativeJsonPath);
        var json = await File.ReadAllTextAsync(jsonPath);
        var datenbestand = JsonSerializer.Deserialize<SeedDatenbestand>(json)
            ?? throw new InvalidOperationException($"Anfangsdatenbestand konnte nicht gelesen werden: {jsonPath}");

        var venueIdMap = new Dictionary<string, Guid>();
        var roomIdMap = new Dictionary<string, Guid>();
        var seatIdMap = new Dictionary<(Guid RoomId, string Row, int Column), Guid>();

        foreach (var seedVenue in datenbestand.Spielstaetten)
        {
            var venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = seedVenue.Name
            };
            venueIdMap[seedVenue.Id] = venue.Id;
            dbContext.Venues.Add(venue);

            foreach (var seedRoom in seedVenue.Raeume)
            {
                var room = new Room
                {
                    Id = Guid.NewGuid(),
                    Name = seedRoom.Name,
                    VenueId = venue.Id,
                    Reihen = seedRoom.Reihen,
                    SpaltenAnzahl = seedRoom.Spalten,
                    GangSpalten = seedRoom.Gang?.Spalten ?? new List<int>()
                };
                roomIdMap[seedRoom.Id] = room.Id;
                dbContext.Rooms.Add(room);

                for (var column = 1; column <= seedRoom.Spalten; column++)
                {
                    if (room.GangSpalten.Contains(column))
                    {
                        continue;
                    }

                    foreach (var reihe in room.Reihen)
                    {
                        var seat = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = reihe, Column = column };
                        seatIdMap[(room.Id, reihe, column)] = seat.Id;
                        dbContext.Seats.Add(seat);
                    }
                }
            }
        }

        var pendingSeedBookings = new List<(SeedVeranstaltung SeedEvent, Guid EventId, Guid FirstPriceCategoryId)>();

        foreach (var seedEvent in datenbestand.Veranstaltungen)
        {
            var @event = new Event
            {
                Id = Guid.NewGuid(),
                Titel = seedEvent.Titel,
                Beschreibung = seedEvent.Beschreibung,
                DauerMinuten = seedEvent.DauerMinuten,
                Altersfreigabe = seedEvent.Altersfreigabe,
                Zeitpunkt = DateTime.SpecifyKind(seedEvent.Zeitpunkt, DateTimeKind.Unspecified),
                VenueId = venueIdMap[seedEvent.SpielstaetteId],
                RoomId = roomIdMap[seedEvent.RaumId]
            };
            dbContext.Events.Add(@event);

            Guid? firstPriceCategoryId = null;
            for (var index = 0; index < seedEvent.Preiskategorien.Count; index++)
            {
                var seedPreiskategorie = seedEvent.Preiskategorien[index];
                var priceCategoryId = Guid.NewGuid();
                if (index == 0)
                {
                    firstPriceCategoryId = priceCategoryId;
                }

                dbContext.PriceCategories.Add(new PriceCategory
                {
                    Id = priceCategoryId,
                    EventId = @event.Id,
                    Name = seedPreiskategorie.Name,
                    Preis = seedPreiskategorie.Preis,
                    Reihenfolge = index
                });
            }

            if (seedEvent.Preiskategorien.Count > 0)
            {
                pendingSeedBookings.Add((seedEvent, @event.Id, firstPriceCategoryId!.Value));
            }
        }

        await dbContext.SaveChangesAsync();

        foreach (var (seedEvent, eventId, firstPriceCategoryId) in pendingSeedBookings)
        {
            if (seedEvent.BelegteSitzplaetze.Count == 0)
            {
                continue;
            }

            var seats = seedEvent.BelegteSitzplaetze
                .Select(label => ParseSeatLabel(label))
                .Select(parsed => seatIdMap[(roomIdMap[seedEvent.RaumId], parsed.Row, parsed.Column)])
                .Select(seatId => new CreateBookingSeatRequestDto(seatId, firstPriceCategoryId))
                .ToList();

            var result = await bookingService.CreateBookingAsync(new CreateBookingRequestDto(
                eventId, "Anfangsbestand", "anfangsbestand@import.lokal", seats));

            if (result.Booking is null)
            {
                throw new InvalidOperationException(
                    $"Seed-Import: belegteSitzplaetze für Veranstaltung {seedEvent.Id} konnten nicht als Buchung angelegt werden ({result.ValidationDetail}).");
            }
        }
    }

    private static (string Row, int Column) ParseSeatLabel(string label)
    {
        var match = Regex.Match(label, "^([A-Za-z]+)(\\d+)$");
        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
    }
}
