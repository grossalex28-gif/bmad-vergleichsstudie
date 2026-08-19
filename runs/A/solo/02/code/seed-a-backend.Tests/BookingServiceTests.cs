using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();

    [Fact]
    public async Task CreateAsync_WithFreeSeats_CreatesBookingAndOccupiesSeats()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var request = new CreateBookingRequestDto(
            TestData.EventId,
            "Ada Lovelace",
            "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId), new CreateBookingSeatDto("A", 2, TestData.CategoryBId)]);

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingResultType.Success, result.Type);
        Assert.NotNull(result.Booking);
        Assert.Equal(8, result.Booking!.Reference.Length);
        Assert.Equal(2, result.Booking.Seats.Count);
        Assert.Equal(50m, result.Booking.Seats.Sum(s => s.Price));
    }

    [Fact]
    public async Task CreateAsync_UnknownEvent_ReturnsEventNotFound()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var request = new CreateBookingRequestDto("does-not-exist", "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]);

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingResultType.EventNotFound, result.Type);
    }

    [Fact]
    public async Task CreateAsync_SeatOutsideRoom_ReturnsInvalidSeat()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var request = new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("Z", 1, TestData.CategoryAId)]);

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingResultType.InvalidSeat, result.Type);
    }

    [Fact]
    public async Task CreateAsync_AisleColumn_ReturnsInvalidSeat()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        // Column 3 is configured as an aisle in TestData.
        var request = new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 3, TestData.CategoryAId)]);

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingResultType.InvalidSeat, result.Type);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSeatInSameRequest_ReturnsInvalidSeat()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var request = new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId), new CreateBookingSeatDto("A", 1, TestData.CategoryBId)]);

        var result = await service.CreateAsync(request);

        Assert.Equal(CreateBookingResultType.InvalidSeat, result.Type);
    }

    [Fact]
    public async Task CreateAsync_SeatAlreadyBooked_ReturnsConflict()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var first = new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]);
        await service.CreateAsync(first);

        var second = new CreateBookingRequestDto(TestData.EventId, "Bob", "bob@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]);
        var result = await service.CreateAsync(second);

        Assert.Equal(CreateBookingResultType.Conflict, result.Type);
        Assert.Contains(("A", 1), result.ConflictingSeats);
    }

    // A-F13: a booking must be rejected in full, not partially, when any single
    // requested seat is already taken.
    [Fact]
    public async Task CreateAsync_OneOfMultipleSeatsAlreadyBooked_RejectsWholeBookingAndLeavesFreeSeatFree()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        await service.CreateAsync(new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]));

        var conflicting = new CreateBookingRequestDto(TestData.EventId, "Bob", "bob@example.com",
            [
                new CreateBookingSeatDto("A", 1, TestData.CategoryAId), // already taken
                new CreateBookingSeatDto("A", 2, TestData.CategoryBId)  // still free
            ]);
        var result = await service.CreateAsync(conflicting);

        Assert.Equal(CreateBookingResultType.Conflict, result.Type);

        // The still-free seat must not have been partially booked.
        var retry = await service.CreateAsync(new CreateBookingRequestDto(TestData.EventId, "Carol", "carol@example.com",
            [new CreateBookingSeatDto("A", 2, TestData.CategoryBId)]));
        Assert.Equal(CreateBookingResultType.Success, retry.Type);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentRequestsForSameSeat_OnlyOneSucceeds()
    {
        await using (var setupDb = _fixture.CreateContext())
        {
            await TestData.SeedSmallEventAsync(setupDb);
        }

        var request = new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]);
        var otherRequest = new CreateBookingRequestDto(TestData.EventId, "Bob", "bob@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]);

        await using var dbOne = _fixture.CreateContext();
        await using var dbTwo = _fixture.CreateContext();
        var serviceOne = new BookingService(dbOne);
        var serviceTwo = new BookingService(dbTwo);

        var results = await Task.WhenAll(
            serviceOne.CreateAsync(request),
            serviceTwo.CreateAsync(otherRequest));

        Assert.Single(results, r => r.Type == CreateBookingResultType.Success);
        Assert.Single(results, r => r.Type == CreateBookingResultType.Conflict);
    }

    [Fact]
    public async Task CancelAsync_FreesSeatsForRebooking()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var created = await service.CreateAsync(new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]));

        var cancelled = await service.CancelAsync(created.Booking!.Reference);
        Assert.NotNull(cancelled);
        Assert.True(cancelled!.IsCancelled);
        Assert.All(cancelled.Seats, s => Assert.True(s.IsCancelled));

        var rebooked = await service.CreateAsync(new CreateBookingRequestDto(TestData.EventId, "Bob", "bob@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]));
        Assert.Equal(CreateBookingResultType.Success, rebooked.Type);
    }

    [Fact]
    public async Task CancelAsync_UnknownReference_ReturnsNull()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var result = await service.CancelAsync("UNKNOWN1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByReferenceAsync_ReturnsBookingWithSeatsAndPriceCategory()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);
        var service = new BookingService(db);

        var created = await service.CreateAsync(new CreateBookingRequestDto(TestData.EventId, "Ada", "ada@example.com",
            [new CreateBookingSeatDto("A", 1, TestData.CategoryAId)]));

        var fetched = await service.GetByReferenceAsync(created.Booking!.Reference);

        Assert.NotNull(fetched);
        Assert.Equal("Kategorie A", fetched!.Seats.Single().PriceCategory.Name);
    }

    public void Dispose() => _fixture.Dispose();
}
