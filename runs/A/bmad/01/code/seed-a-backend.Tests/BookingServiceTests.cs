using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class BookingServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Event Event, Seat SeatA1, Seat SeatA2, PriceCategory CategoryA, PriceCategory CategoryB)> SeedEventWithSeatsAsync(AppDbContext dbContext)
    {
        var venue = new Venue { Id = Guid.NewGuid(), Name = "Stadthalle Nordpark" };
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = "Kleiner Saal",
            VenueId = venue.Id,
            Reihen = new List<string> { "A" },
            SpaltenAnzahl = 2,
            GangSpalten = new List<int>()
        };
        dbContext.Venues.Add(venue);
        dbContext.Rooms.Add(room);

        var seatA1 = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = "A", Column = 1 };
        var seatA2 = new Seat { Id = Guid.NewGuid(), RoomId = room.Id, Row = "A", Column = 2 };
        dbContext.Seats.Add(seatA1);
        dbContext.Seats.Add(seatA2);

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

        var categoryA = new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie A", Preis = 32.00m, Reihenfolge = 0 };
        var categoryB = new PriceCategory { Id = Guid.NewGuid(), EventId = @event.Id, Name = "Kategorie B", Preis = 22.00m, Reihenfolge = 1 };
        dbContext.PriceCategories.Add(categoryA);
        dbContext.PriceCategories.Add(categoryB);

        await dbContext.SaveChangesAsync();

        return (@event, seatA1, seatA2, categoryA, categoryB);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidRequest_CreatesBookingAndReturnsReferenceAndTotalPrice()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_WithValidRequest_CreatesBookingAndReturnsReferenceAndTotalPrice));
        var (@event, seatA1, seatA2, categoryA, categoryB) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto>
            {
                new(seatA1.Id, categoryA.Id),
                new(seatA2.Id, categoryB.Id)
            }));

        Assert.NotNull(result.Booking);
        Assert.Equal(26, result.Booking!.Reference.Length);
        Assert.Equal(54.00m, result.Booking.TotalPrice);
        Assert.Equal("Active", result.Booking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_MissingName_ReturnsInvalidWithNameField()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_MissingName_ReturnsInvalidWithNameField));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, null, "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));

        Assert.Equal(new List<string> { "name" }, result.InvalidFields);
    }

    [Fact]
    public async Task CreateBookingAsync_MissingEmail_ReturnsInvalidWithEmailField()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_MissingEmail_ReturnsInvalidWithEmailField));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", null,
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));

        Assert.Equal(new List<string> { "email" }, result.InvalidFields);
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeats_ReturnsInvalidWithSeatsField()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_NoSeats_ReturnsInvalidWithSeatsField));
        var (@event, _, _, _, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com", new List<CreateBookingSeatRequestDto>()));

        Assert.Equal(new List<string> { "seats" }, result.InvalidFields);
    }

    [Fact]
    public async Task CreateBookingAsync_UnknownPriceCategoryForEvent_ReturnsInvalidWithSeatsField()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_UnknownPriceCategoryForEvent_ReturnsInvalidWithSeatsField));
        var (@event, seatA1, _, _, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, Guid.NewGuid()) }));

        Assert.Equal(new List<string> { "seats" }, result.InvalidFields);
    }

    [Fact]
    public async Task CreateBookingAsync_SeatAlreadyActivelyBooked_ReturnsConflictWithSeatLabel()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_SeatAlreadyActivelyBooked_ReturnsConflictWithSeatLabel));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);

        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            Reference = "EXISTINGBOOKINGREFERENCE1",
            EventId = @event.Id,
            Name = "Bestandsbuchung",
            Email = "bestand@example.com",
            Status = BookingStatus.Active
        };
        dbContext.Bookings.Add(existingBooking);
        dbContext.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = existingBooking.Id,
            EventId = @event.Id,
            SeatId = seatA1.Id,
            PriceCategoryId = categoryA.Id,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);
        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));

        Assert.Null(result.Booking);
        Assert.Equal(new List<string> { "A1" }, result.ConflictingSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConflictOnOneOfTwoSeats_RejectsBothNoPartialBooking()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_ConflictOnOneOfTwoSeats_RejectsBothNoPartialBooking));
        var (@event, seatA1, seatA2, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);

        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            Reference = "EXISTINGBOOKINGREFERENCE2",
            EventId = @event.Id,
            Name = "Bestandsbuchung",
            Email = "bestand@example.com",
            Status = BookingStatus.Active
        };
        dbContext.Bookings.Add(existingBooking);
        dbContext.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = existingBooking.Id,
            EventId = @event.Id,
            SeatId = seatA1.Id,
            PriceCategoryId = categoryA.Id,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);
        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id), new(seatA2.Id, categoryA.Id) }));

        Assert.Null(result.Booking);
        var seatA2StillFree = !await dbContext.BookingSeats.AnyAsync(bs => bs.SeatId == seatA2.Id && bs.IsActive);
        Assert.True(seatA2StillFree);
    }

    [Fact]
    public async Task CreateBookingAsync_InactiveBookingSeatForSameSeat_DoesNotBlockNewBooking()
    {
        using var dbContext = CreateContext(nameof(CreateBookingAsync_InactiveBookingSeatForSameSeat_DoesNotBlockNewBooking));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);

        var cancelledBooking = new Booking
        {
            Id = Guid.NewGuid(),
            Reference = "CANCELLEDBOOKINGREFERENCE",
            EventId = @event.Id,
            Name = "Stornierte Buchung",
            Email = "storno@example.com",
            Status = BookingStatus.Cancelled
        };
        dbContext.Bookings.Add(cancelledBooking);
        dbContext.BookingSeats.Add(new BookingSeat
        {
            Id = Guid.NewGuid(),
            BookingId = cancelledBooking.Id,
            EventId = @event.Id,
            SeatId = seatA1.Id,
            PriceCategoryId = categoryA.Id,
            IsActive = false
        });
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);
        var result = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));

        Assert.NotNull(result.Booking);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_ExistingReference_ReturnsBookingWithEventVenueAndCategoryDetails()
    {
        using var dbContext = CreateContext(nameof(GetBookingByReferenceAsync_ExistingReference_ReturnsBookingWithEventVenueAndCategoryDetails));
        var (@event, seatA1, seatA2, categoryA, categoryB) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var created = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto>
            {
                new(seatA1.Id, categoryA.Id),
                new(seatA2.Id, categoryB.Id)
            }));

        var result = await service.GetBookingByReferenceAsync(created.Booking!.Reference);

        Assert.NotNull(result);
        Assert.Equal("Kammerkonzert", result!.EventTitle);
        Assert.Equal("Stadthalle Nordpark", result.VenueName);
        Assert.Equal(new[] { "Kategorie A", "Kategorie B" }, result.Seats.Select(s => s.PriceCategoryName).OrderBy(n => n));
        Assert.Equal(created.Booking.TotalPrice, result.TotalPrice);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_UnknownReference_ReturnsNull()
    {
        using var dbContext = CreateContext(nameof(GetBookingByReferenceAsync_UnknownReference_ReturnsNull));
        await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.GetBookingByReferenceAsync("UNBEKANNTEREFERENZ1234");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingByReferenceAsync_ReferenceWithSurroundingWhitespace_StillFindsBooking()
    {
        using var dbContext = CreateContext(nameof(GetBookingByReferenceAsync_ReferenceWithSurroundingWhitespace_StillFindsBooking));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var created = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));

        var result = await service.GetBookingByReferenceAsync($"  {created.Booking!.Reference}  ");

        Assert.NotNull(result);
        Assert.Equal(created.Booking.Reference, result!.Reference);
    }

    [Fact]
    public async Task CancelBookingAsync_ActiveBooking_SetsStatusCancelledAndSeatsInactive()
    {
        using var dbContext = CreateContext(nameof(CancelBookingAsync_ActiveBooking_SetsStatusCancelledAndSeatsInactive));
        var (@event, seatA1, seatA2, categoryA, categoryB) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var created = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto>
            {
                new(seatA1.Id, categoryA.Id),
                new(seatA2.Id, categoryB.Id)
            }));

        var result = await service.CancelBookingAsync(created.Booking!.Reference);

        Assert.NotNull(result.Booking);
        Assert.Equal("Cancelled", result.Booking!.Status);
        var persisted = await dbContext.Bookings.SingleAsync(b => b.Reference == created.Booking.Reference);
        Assert.Equal(BookingStatus.Cancelled, persisted.Status);
        var seatsStillActive = await dbContext.BookingSeats.AnyAsync(bs => bs.BookingId == persisted.Id && bs.IsActive);
        Assert.False(seatsStillActive);
    }

    [Fact]
    public async Task CancelBookingAsync_UnknownReference_ReturnsNotFound()
    {
        using var dbContext = CreateContext(nameof(CancelBookingAsync_UnknownReference_ReturnsNotFound));
        await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var result = await service.CancelBookingAsync("UNBEKANNTEREFERENZ1234");

        Assert.True(result.NotFound);
        Assert.Null(result.Booking);
    }

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelledBooking_ReturnsAlreadyCancelledWithoutChangingState()
    {
        using var dbContext = CreateContext(nameof(CancelBookingAsync_AlreadyCancelledBooking_ReturnsAlreadyCancelledWithoutChangingState));
        var (@event, seatA1, _, categoryA, _) = await SeedEventWithSeatsAsync(dbContext);
        var service = new BookingService(dbContext);

        var created = await service.CreateBookingAsync(new CreateBookingRequestDto(
            @event.Id, "Max Mustermann", "max@example.com",
            new List<CreateBookingSeatRequestDto> { new(seatA1.Id, categoryA.Id) }));
        await service.CancelBookingAsync(created.Booking!.Reference);

        var result = await service.CancelBookingAsync(created.Booking.Reference);

        Assert.True(result.AlreadyCancelled);
        Assert.Null(result.Booking);
    }
}
