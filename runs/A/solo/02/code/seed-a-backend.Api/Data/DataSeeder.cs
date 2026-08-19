using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public static partial class DataSeeder
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, string seedFilePath, CancellationToken cancellationToken = default)
    {
        if (await db.Venues.AnyAsync(cancellationToken))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(seedFilePath, cancellationToken);
        var root = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        foreach (var seedVenue in root.Spielstaetten)
        {
            var venue = new Venue
            {
                Id = seedVenue.Id,
                Name = seedVenue.Name
            };

            foreach (var seedRoom in seedVenue.Raeume)
            {
                venue.Rooms.Add(new Room
                {
                    Id = seedRoom.Id,
                    Name = seedRoom.Name,
                    VenueId = venue.Id,
                    RowLabels = seedRoom.Reihen,
                    Columns = seedRoom.Spalten,
                    AisleColumns = seedRoom.Gang?.Spalten ?? new List<int>()
                });
            }

            db.Venues.Add(venue);
        }

        foreach (var seedEvent in root.Veranstaltungen)
        {
            var @event = new Event
            {
                Id = seedEvent.Id,
                Title = seedEvent.Titel,
                Description = seedEvent.Beschreibung,
                VenueId = seedEvent.SpielstaetteId,
                RoomId = seedEvent.RaumId,
                StartsAt = seedEvent.Zeitpunkt,
                DurationMinutes = seedEvent.DauerMinuten,
                AgeRating = seedEvent.Altersfreigabe
            };

            foreach (var seedCategory in seedEvent.Preiskategorien)
            {
                @event.PriceCategories.Add(new PriceCategory
                {
                    Id = seedCategory.Id,
                    Name = seedCategory.Name,
                    Price = seedCategory.Preis,
                    EventId = @event.Id
                });
            }

            db.Events.Add(@event);

            if (seedEvent.BelegteSitzplaetze.Count > 0)
            {
                var defaultCategoryId = seedEvent.Preiskategorien.First().Id;
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    Reference = BookingReferenceGenerator.Generate(),
                    EventId = @event.Id,
                    CustomerName = "Vorbelegung",
                    CustomerEmail = "vorbelegung@system.local",
                    CreatedAt = DateTime.UtcNow,
                    IsCancelled = false
                };

                foreach (var seat in seedEvent.BelegteSitzplaetze)
                {
                    var (row, column) = ParseSeat(seat);
                    booking.Seats.Add(new BookingSeat
                    {
                        BookingId = booking.Id,
                        EventId = @event.Id,
                        RowLabel = row,
                        Column = column,
                        PriceCategoryId = defaultCategoryId,
                        Price = seedEvent.Preiskategorien.First().Preis,
                        IsCancelled = false
                    });
                }

                db.Bookings.Add(booking);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static (string Row, int Column) ParseSeat(string seat)
    {
        var match = SeatPattern().Match(seat);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Ungültige Sitzplatzbezeichnung im Anfangsdatenbestand: '{seat}'.");
        }

        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
    }

    [GeneratedRegex("^([A-Za-z]+)([0-9]+)$")]
    private static partial Regex SeatPattern();
}
