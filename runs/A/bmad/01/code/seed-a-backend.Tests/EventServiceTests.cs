using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class EventServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsEventsSortedByZeitpunktWithVenueName()
    {
        using var dbContext = CreateContext(nameof(GetEventsAsync_ReturnsEventsSortedByZeitpunktWithVenueName));

        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Kleiner Saal", VenueId = venue.Id };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);

        var laterEvent = new Event
        {
            Id = Guid.NewGuid(),
            Titel = "Später",
            Beschreibung = "b",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTime(2026, 10, 1, 20, 0, 0, DateTimeKind.Unspecified),
            VenueId = venue.Id,
            RoomId = room.Id
        };
        var earlierEvent = new Event
        {
            Id = Guid.NewGuid(),
            Titel = "Früher",
            Beschreibung = "b",
            DauerMinuten = 60,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTime(2026, 9, 1, 19, 0, 0, DateTimeKind.Unspecified),
            VenueId = venue.Id,
            RoomId = room.Id
        };
        dbContext.Events.AddRange(laterEvent, earlierEvent);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Früher", result[0].Title);
        Assert.Equal("Später", result[1].Title);
        Assert.Equal("Stadthalle Nordpark", result[0].VenueName);
    }

    private static async Task<(AppDbContext dbContext, Guid venueId, Guid roomId)> SeedVenueAndRoomAsync(string dbName)
    {
        var dbContext = CreateContext(dbName);
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Kleiner Saal", VenueId = venue.Id };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();
        return (dbContext, venue.Id, room.Id);
    }

    private static Event MakeEvent(string title, DateTime zeitpunkt, Guid venueId, Guid roomId) => new()
    {
        Id = Guid.NewGuid(),
        Titel = title,
        Beschreibung = "b",
        DauerMinuten = 60,
        Altersfreigabe = 0,
        Zeitpunkt = zeitpunkt,
        VenueId = venueId,
        RoomId = roomId
    };

    [Fact]
    public async Task GetEventsAsync_WithFromAndTo_IncludesEventsExactlyOnBoundaryDates()
    {
        var (dbContext, venueId, roomId) = await SeedVenueAndRoomAsync(nameof(GetEventsAsync_WithFromAndTo_IncludesEventsExactlyOnBoundaryDates));
        using var _ = dbContext;

        var onFrom = MakeEvent("Am Von-Tag früh", new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        var onTo = MakeEvent("Am Bis-Tag spät", new DateTime(2026, 9, 12, 23, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        dbContext.Events.AddRange(onFrom, onTo);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 12));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Title == "Am Von-Tag früh");
        Assert.Contains(result, e => e.Title == "Am Bis-Tag spät");
    }

    [Fact]
    public async Task GetEventsAsync_WithFromAndTo_ExcludesEventsOutsideRange()
    {
        var (dbContext, venueId, roomId) = await SeedVenueAndRoomAsync(nameof(GetEventsAsync_WithFromAndTo_ExcludesEventsOutsideRange));
        using var _ = dbContext;

        var beforeFrom = MakeEvent("Vor Von", new DateTime(2026, 9, 4, 23, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        var afterTo = MakeEvent("Nach Bis", new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        dbContext.Events.AddRange(beforeFrom, afterTo);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 12));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventsAsync_WithOnlyFrom_ReturnsAllEventsFromDateOnwards()
    {
        var (dbContext, venueId, roomId) = await SeedVenueAndRoomAsync(nameof(GetEventsAsync_WithOnlyFrom_ReturnsAllEventsFromDateOnwards));
        using var _ = dbContext;

        var before = MakeEvent("Davor", new DateTime(2026, 9, 4, 23, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        var onFrom = MakeEvent("Am Von-Tag", new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        var farAfter = MakeEvent("Weit danach", new DateTime(2026, 10, 10, 22, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        dbContext.Events.AddRange(before, onFrom, farAfter);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(from: new DateOnly(2026, 9, 5));

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "Davor");
    }

    [Fact]
    public async Task GetEventsAsync_WithOnlyTo_ReturnsAllEventsUpToDateInclusive()
    {
        var (dbContext, venueId, roomId) = await SeedVenueAndRoomAsync(nameof(GetEventsAsync_WithOnlyTo_ReturnsAllEventsUpToDateInclusive));
        using var _ = dbContext;

        var farBefore = MakeEvent("Weit davor", new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified), venueId, roomId);
        var onTo = MakeEvent("Am Bis-Tag spät", new DateTime(2026, 9, 12, 23, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        var after = MakeEvent("Danach", new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Unspecified), venueId, roomId);
        dbContext.Events.AddRange(farBefore, onTo, after);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(to: new DateOnly(2026, 9, 12));

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "Danach");
    }

    [Fact]
    public async Task GetEventsAsync_WithoutFilter_ReturnsAllEvents()
    {
        var (dbContext, venueId, roomId) = await SeedVenueAndRoomAsync(nameof(GetEventsAsync_WithoutFilter_ReturnsAllEvents));
        using var _ = dbContext;

        dbContext.Events.AddRange(
            MakeEvent("Eins", new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified), venueId, roomId),
            MakeEvent("Zwei", new DateTime(2026, 10, 10, 22, 0, 0, DateTimeKind.Unspecified), venueId, roomId));
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync();

        Assert.Equal(2, result.Count);
    }

    private static async Task<(AppDbContext dbContext, Venue venueA, Room roomA, Venue venueB, Room roomB)> SeedTwoVenuesAsync(string dbName)
    {
        var dbContext = CreateContext(dbName);
        var venueA = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var roomA = new Room { Id = Guid.NewGuid(), Name = "Großer Saal", VenueId = venueA.Id };
        var venueB = new Venue { Id = Guid.NewGuid(), Name = "Kulturhaus Südtor" };
        var roomB = new Room { Id = Guid.NewGuid(), Name = "Konzertsaal", VenueId = venueB.Id };
        dbContext.Venues.AddRange(venueA, venueB);
        dbContext.Rooms.AddRange(roomA, roomB);
        await dbContext.SaveChangesAsync();
        return (dbContext, venueA, roomA, venueB, roomB);
    }

    [Fact]
    public async Task GetEventsAsync_WithVenueId_ReturnsOnlyEventsOfThatVenue()
    {
        var (dbContext, venueA, roomA, venueB, roomB) = await SeedTwoVenuesAsync(nameof(GetEventsAsync_WithVenueId_ReturnsOnlyEventsOfThatVenue));
        using var _ = dbContext;

        var eventA = MakeEvent("Konzert A", new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified), venueA.Id, roomA.Id);
        var eventB = MakeEvent("Konzert B", new DateTime(2026, 9, 6, 19, 30, 0, DateTimeKind.Unspecified), venueB.Id, roomB.Id);
        dbContext.Events.AddRange(eventA, eventB);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(venueId: venueA.Id);

        Assert.Single(result);
        Assert.Equal("Konzert A", result[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_WithVenueIdAndDateRange_CombinesBothConditionsWithAnd()
    {
        var (dbContext, venueA, roomA, venueB, roomB) = await SeedTwoVenuesAsync(nameof(GetEventsAsync_WithVenueIdAndDateRange_CombinesBothConditionsWithAnd));
        using var _ = dbContext;

        var wrongVenueInRange = MakeEvent("Falsche Spielstätte, im Zeitraum", new DateTime(2026, 9, 6, 19, 30, 0, DateTimeKind.Unspecified), venueB.Id, roomB.Id);
        var rightVenueOutOfRange = MakeEvent("Richtige Spielstätte, außerhalb Zeitraum", new DateTime(2026, 10, 6, 19, 30, 0, DateTimeKind.Unspecified), venueA.Id, roomA.Id);
        var rightVenueInRange = MakeEvent("Richtige Spielstätte, im Zeitraum", new DateTime(2026, 9, 7, 19, 30, 0, DateTimeKind.Unspecified), venueA.Id, roomA.Id);
        dbContext.Events.AddRange(wrongVenueInRange, rightVenueOutOfRange, rightVenueInRange);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), venueA.Id);

        Assert.Single(result);
        Assert.Equal("Richtige Spielstätte, im Zeitraum", result[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_WithUnknownVenueId_ReturnsEmptyResultWithoutError()
    {
        var (dbContext, venueA, roomA, _, _) = await SeedTwoVenuesAsync(nameof(GetEventsAsync_WithUnknownVenueId_ReturnsEmptyResultWithoutError));
        using var _ = dbContext;

        dbContext.Events.Add(MakeEvent("Konzert A", new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified), venueA.Id, roomA.Id));
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync(venueId: Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEventsAsync_WithoutVenueId_ReturnsAllEvents()
    {
        var (dbContext, venueA, roomA, venueB, roomB) = await SeedTwoVenuesAsync(nameof(GetEventsAsync_WithoutVenueId_ReturnsAllEvents));
        using var _ = dbContext;

        dbContext.Events.AddRange(
            MakeEvent("Konzert A", new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified), venueA.Id, roomA.Id),
            MakeEvent("Konzert B", new DateTime(2026, 9, 6, 19, 30, 0, DateTimeKind.Unspecified), venueB.Id, roomB.Id));
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsFullDetailForExistingEvent()
    {
        using var dbContext = CreateContext(nameof(GetEventByIdAsync_ReturnsFullDetailForExistingEvent));

        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Kleiner Saal", VenueId = venue.Id };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Titel = "Kammerkonzert Frühling",
            Beschreibung = "Ein stimmungsvolles Kammerkonzert.",
            DauerMinuten = 90,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified),
            VenueId = venue.Id,
            RoomId = room.Id
        };
        dbContext.Events.Add(eventEntity);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetEventByIdAsync(eventEntity.Id);

        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id, result!.Id);
        Assert.Equal("Kammerkonzert Frühling", result.Title);
        Assert.Equal("Ein stimmungsvolles Kammerkonzert.", result.Description);
        Assert.Equal(90, result.DurationMinutes);
        Assert.Equal(0, result.AgeRating);
        Assert.Equal("Stadthalle Nordpark", result.VenueName);
        Assert.Equal("Kleiner Saal", result.RoomName);
        Assert.Equal(eventEntity.Zeitpunkt, result.StartsAt);
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsNullForUnknownId()
    {
        using var dbContext = CreateContext(nameof(GetEventByIdAsync_ReturnsNullForUnknownId));

        var service = new EventService(dbContext);
        var result = await service.GetEventByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVenuesAsync_ReturnsAllVenuesSortedByName()
    {
        using var dbContext = CreateContext(nameof(GetVenuesAsync_ReturnsAllVenuesSortedByName));
        var venueB = new Venue { Id = Guid.NewGuid(), Name = "Kulturhaus Südtor" };
        var venueA = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        dbContext.Venues.AddRange(venueB, venueA);
        await dbContext.SaveChangesAsync();

        var service = new EventService(dbContext);
        var result = await service.GetVenuesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Kulturhaus Südtor", result[0].Name);
        Assert.Equal("Stadthalle Nordpark", result[1].Name);
    }
}
