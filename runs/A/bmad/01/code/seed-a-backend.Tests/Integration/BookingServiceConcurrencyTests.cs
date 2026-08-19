using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests.Integration;

[Collection("SqlServer")]
public class BookingServiceConcurrencyTests(SqlServerFixture fixture)
{
    private async Task<(Guid EventId, Guid SeatAId, Guid SeatBId, Guid SeatCId, Guid CategoryId)> SeedEventWithSeatsAsync()
    {
        await using var dbContext = fixture.CreateContext();

        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Kleiner Saal",
            VenueId = venue.Id,
            Reihen = new List<string> { "A" },
            SpaltenAnzahl = 3,
            GangSpalten = new List<int>()
        };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);

        var seatA = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = "A", Column = 1 };
        var seatB = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = "A", Column = 2 };
        var seatC = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = "A", Column = 3 };
        dbContext.Seats.Add(seatA);
        dbContext.Seats.Add(seatB);
        dbContext.Seats.Add(seatC);

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

        var category = new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie A", Preis = 32.00m, Reihenfolge = 0 };
        dbContext.PriceCategories.Add(category);

        await dbContext.SaveChangesAsync();

        return (@event.Id, seatA.Id, seatB.Id, seatC.Id, category.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_TwoConcurrentRequestsForSameSeat_ExactlyOneSucceeds()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("SQL Server nicht verfügbar (ConnectionStrings__Default nicht gesetzt).");
        }

        var (eventId, seatId, _, _, categoryId) = await SeedEventWithSeatsAsync();

        async Task<CreateBookingResult> BookAsync()
        {
            await using var dbContext = fixture.CreateContext();
            var service = new BookingService(dbContext);
            return await service.CreateBookingAsync(new CreateBookingRequestDto(
                eventId, "Besucher", "besucher@example.com",
                new List<CreateBookingSeatRequestDto> { new(seatId, categoryId) }));
        }

        CreateBookingResult[] results = [];
        for (var i = 0; i < 10 && results.Count(r => r.Booking is not null) != 1; i++)
        {
            results = await Task.WhenAll(BookAsync(), BookAsync());
            if (results.Count(r => r.Booking is not null) == 1)
            {
                break;
            }

            // Konflikt-Timing verfehlt (beide seriell ausgeführt) — Zustand zurücksetzen und erneut versuchen.
            await using var cleanupContext = fixture.CreateContext();
            await cleanupContext.BookingSeats.Where(bs => bs.SeatId == seatId).ExecuteDeleteAsync();
            await cleanupContext.Bookings.Where(b => b.EventId == eventId).ExecuteDeleteAsync();
        }

        Assert.Equal(1, results.Count(r => r.Booking is not null));
        Assert.Equal(1, results.Count(r => r.ConflictingSeats is not null));

        await using var verifyContext = fixture.CreateContext();
        var activeCount = await verifyContext.BookingSeats.CountAsync(bs => bs.SeatId == seatId && bs.IsActive);
        Assert.Equal(1, activeCount);
    }

    [Fact]
    public async Task CreateBookingAsync_TwoConcurrentRequestsForDisjointSeats_BothSucceed()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("SQL Server nicht verfügbar (ConnectionStrings__Default nicht gesetzt).");
        }

        var (eventId, seatAId, seatBId, _, categoryId) = await SeedEventWithSeatsAsync();

        async Task<CreateBookingResult> BookAsync(Guid seatId)
        {
            await using var dbContext = fixture.CreateContext();
            var service = new BookingService(dbContext);
            return await service.CreateBookingAsync(new CreateBookingRequestDto(
                eventId, "Besucher", "besucher@example.com",
                new List<CreateBookingSeatRequestDto> { new(seatId, categoryId) }));
        }

        var results = await Task.WhenAll(BookAsync(seatAId), BookAsync(seatBId));

        Assert.All(results, r => Assert.NotNull(r.Booking));
    }

    [Fact]
    public async Task CreateBookingAsync_ThreeSeatsOneConflicting_RejectsAllNotJustConflictingOne()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("SQL Server nicht verfügbar (ConnectionStrings__Default nicht gesetzt).");
        }

        var (eventId, seatAId, seatBId, seatCId, categoryId) = await SeedEventWithSeatsAsync();

        await using (var otherProcessContext = fixture.CreateContext())
        {
            var otherService = new BookingService(otherProcessContext);
            var otherResult = await otherService.CreateBookingAsync(new CreateBookingRequestDto(
                eventId, "Anderer Besucher", "anderer@example.com",
                new List<CreateBookingSeatRequestDto> { new(seatBId, categoryId) }));
            Assert.NotNull(otherResult.Booking);
        }

        await using var dbContext = fixture.CreateContext();
        var service = new BookingService(dbContext);
        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            eventId, "Besucher", "besucher@example.com",
            new List<CreateBookingSeatRequestDto>
            {
                new(seatAId, categoryId),
                new(seatBId, categoryId),
                new(seatCId, categoryId)
            }));

        Assert.Null(result.Booking);
        Assert.Contains("A2", result.ConflictingSeats);

        await using var verifyContext = fixture.CreateContext();
        Assert.False(await verifyContext.BookingSeats.AnyAsync(bs => bs.SeatId == seatAId && bs.IsActive));
        Assert.False(await verifyContext.BookingSeats.AnyAsync(bs => bs.SeatId == seatCId && bs.IsActive));
    }
}
