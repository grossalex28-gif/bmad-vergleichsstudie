using seed_a_backend.Api.Data;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Tests;

public static class TestData
{
    public const string VenueId = "V1";
    public const string RoomId = "R1";
    public const string EventId = "E1";
    public const string CategoryAId = "E1-A";
    public const string CategoryBId = "E1-B";

    public static async Task<Event> SeedSmallEventAsync(AppDbContext db)
    {
        var venue = new Venue { Id = VenueId, Name = "Stadthalle Nordpark" };
        var room = new Room
        {
            Id = RoomId,
            Name = "Kleiner Saal",
            VenueId = VenueId,
            RowLabels = ["A", "B"],
            Columns = 5,
            AisleColumns = [3]
        };
        venue.Rooms.Add(room);

        var @event = new Event
        {
            Id = EventId,
            Title = "Testkonzert",
            Description = "Ein Testereignis.",
            VenueId = VenueId,
            RoomId = RoomId,
            StartsAt = new DateTime(2026, 9, 1, 19, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 90,
            AgeRating = 0
        };
        @event.PriceCategories.Add(new PriceCategory { Id = CategoryAId, Name = "Kategorie A", Price = 30m, EventId = EventId });
        @event.PriceCategories.Add(new PriceCategory { Id = CategoryBId, Name = "Kategorie B", Price = 20m, EventId = EventId });

        db.Venues.Add(venue);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        return @event;
    }
}
