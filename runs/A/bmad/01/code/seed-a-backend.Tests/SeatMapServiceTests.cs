using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class SeatMapServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Venue Venue, Room Room, Event Event)> SeedRoomWithEventAsync(AppDbContext dbContext)
    {
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Kleiner Saal",
            VenueId = venue.Id,
            Reihen = new List<string> { "A", "B" },
            SpaltenAnzahl = 3,
            GangSpalten = new List<int> { 2 }
        };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);

        foreach (var reihe in room.Reihen)
        {
            dbContext.Seats.Add(new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = reihe, Column = 1 });
            dbContext.Seats.Add(new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = reihe, Column = 3 });
        }

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Titel = "Kammerkonzert",
            Beschreibung = "b",
            DauerMinuten = 90,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTime(2026, 9, 5, 19, 30, 0, DateTimeKind.Unspecified),
            VenueId = venue.Id,
            RoomId = room.Id
        };
        dbContext.Events.Add(@event);

        await dbContext.SaveChangesAsync();

        return (venue, room, @event);
    }

    [Fact]
    public async Task GetSeatMapAsync_ReturnsFullGridWithAisleColumnsAndFreeSeats()
    {
        using var dbContext = CreateContext(nameof(GetSeatMapAsync_ReturnsFullGridWithAisleColumnsAndFreeSeats));
        var (_, _, @event) = await SeedRoomWithEventAsync(dbContext);

        var service = new SeatMapService(dbContext);
        var result = await service.GetSeatMapAsync(@event.Id);

        Assert.NotNull(result);
        Assert.Equal(new List<string> { "A", "B" }, result!.Rows);
        Assert.Equal(3, result.Columns);
        Assert.Equal(new List<int> { 2 }, result.AisleColumns);
        Assert.Equal(4, result.Seats.Count);
        Assert.All(result.Seats, s => Assert.Equal(SeatStatus.Free, s.Status));
        Assert.Empty(result.PriceCategories);
    }

    [Fact]
    public async Task GetSeatMapAsync_ReturnsPriceCategoriesOrderedByReihenfolge()
    {
        using var dbContext = CreateContext(nameof(GetSeatMapAsync_ReturnsPriceCategoriesOrderedByReihenfolge));
        var (_, _, @event) = await SeedRoomWithEventAsync(dbContext);

        dbContext.PriceCategories.Add(new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie B", Preis = 22.00m, Reihenfolge = 1 });
        dbContext.PriceCategories.Add(new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie A", Preis = 32.00m, Reihenfolge = 0 });
        await dbContext.SaveChangesAsync();

        var service = new SeatMapService(dbContext);
        var result = await service.GetSeatMapAsync(@event.Id);

        Assert.NotNull(result);
        Assert.Equal(new[] { "Kategorie A", "Kategorie B" }, result!.PriceCategories.Select(pc => pc.Name));
        Assert.Equal(new[] { 32.00m, 22.00m }, result.PriceCategories.Select(pc => pc.Price));
    }

    [Fact]
    public async Task GetSeatMapAsync_ReturnsNullForUnknownEventId()
    {
        using var dbContext = CreateContext(nameof(GetSeatMapAsync_ReturnsNullForUnknownEventId));

        var service = new SeatMapService(dbContext);
        var result = await service.GetSeatMapAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeatMapAsync_SeatWithActiveBookingSeat_ReturnsOccupiedStatus()
    {
        using var dbContext = CreateContext(nameof(GetSeatMapAsync_SeatWithActiveBookingSeat_ReturnsOccupiedStatus));
        var (_, _, @event) = await SeedRoomWithEventAsync(dbContext);
        var priceCategory = new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie A", Preis = 32.00m, Reihenfolge = 0 };
        dbContext.PriceCategories.Add(priceCategory);

        var seats = await dbContext.Seats.Where(s => s.RoomId == @event.RoomId).OrderBy(s => s.Row).ThenBy(s => s.Column).ToListAsync();
        var occupiedSeat = seats[0];
        var freeSeat = seats[1];

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Reference = "SEATMAPTESTBOOKINGREFEREN",
            EventId = @event.Id,
            Name = "Test",
            Email = "test@example.com",
            Status = BookingStatus.Active
        };
        dbContext.Bookings.Add(booking);
        dbContext.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            EventId = @event.Id,
            SeatId = occupiedSeat.Id,
            PriceCategoryId = priceCategory.Id,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new SeatMapService(dbContext);
        var result = await service.GetSeatMapAsync(@event.Id);

        Assert.NotNull(result);
        Assert.Equal(SeatStatus.Occupied, result!.Seats.Single(s => s.SeatId == occupiedSeat.Id).Status);
        Assert.Equal(SeatStatus.Free, result.Seats.Single(s => s.SeatId == freeSeat.Id).Status);
    }

    [Fact]
    public async Task GetSeatMapAsync_TwoEventsInSameRoom_ReturnSameSeatIds()
    {
        using var dbContext = CreateContext(nameof(GetSeatMapAsync_TwoEventsInSameRoom_ReturnSameSeatIds));
        var (venue, room, firstEvent) = await SeedRoomWithEventAsync(dbContext);

        var secondEvent = new Event
        {
            Id = Guid.NewGuid(),
            Titel = "Zweite Vorstellung",
            Beschreibung = "b",
            DauerMinuten = 90,
            Altersfreigabe = 0,
            Zeitpunkt = new DateTime(2026, 9, 12, 19, 30, 0, DateTimeKind.Unspecified),
            VenueId = venue.Id,
            RoomId = room.Id
        };
        dbContext.Events.Add(secondEvent);
        await dbContext.SaveChangesAsync();

        var service = new SeatMapService(dbContext);
        var firstResult = await service.GetSeatMapAsync(firstEvent.Id);
        var secondResult = await service.GetSeatMapAsync(secondEvent.Id);

        var firstSeatIds = firstResult!.Seats.Select(s => s.SeatId).ToHashSet();
        var secondSeatIds = secondResult!.Seats.Select(s => s.SeatId).ToHashSet();

        Assert.Equal(firstSeatIds, secondSeatIds);
    }
}
