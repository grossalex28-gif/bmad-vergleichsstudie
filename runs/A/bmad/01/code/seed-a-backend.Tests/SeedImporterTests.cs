using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Tests;

public class SeedImporterTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ImportAsync_OnEmptyDatabase_ImportsVenuesRoomsAndEvents()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_OnEmptyDatabase_ImportsVenuesRoomsAndEvents));

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        Assert.Equal(2, await dbContext.Venues.CountAsync());
        Assert.Equal(4, await dbContext.Rooms.CountAsync());
        Assert.Equal(6, await dbContext.Events.CountAsync());
        Assert.Equal(384, await dbContext.Seats.CountAsync());

        var e1 = await dbContext.Events
            .Include(e => e.Venue)
            .Include(e => e.Room)
            .SingleAsync(e => e.Titel == "Kammerkonzert Frühling");

        Assert.Equal("Stadthalle Nordpark", e1.Venue!.Name);
        Assert.Equal("Kleiner Saal", e1.Room!.Name);

        var priceCategories = await dbContext.PriceCategories
            .Where(pc => pc.EventId == e1.Id)
            .OrderBy(pc => pc.Reihenfolge)
            .ToListAsync();
        Assert.Equal(new[] { "Kategorie A", "Kategorie B" }, priceCategories.Select(pc => pc.Name));
        Assert.Equal(new[] { 32.00m, 22.00m }, priceCategories.Select(pc => pc.Preis));
    }

    [Fact]
    public async Task ImportAsync_WhenDataAlreadyExists_DoesNotImportAgain()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_WhenDataAlreadyExists_DoesNotImportAgain));
        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        Assert.Equal(2, await dbContext.Venues.CountAsync());
        Assert.Equal(4, await dbContext.Rooms.CountAsync());
        Assert.Equal(6, await dbContext.Events.CountAsync());
        Assert.Equal(384, await dbContext.Seats.CountAsync());
        Assert.Equal(12, await dbContext.PriceCategories.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_ImportsExactlyTwoPriceCategoriesPerEvent()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_ImportsExactlyTwoPriceCategoriesPerEvent));

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        Assert.Equal(12, await dbContext.PriceCategories.CountAsync());
        Assert.Equal(0, await dbContext.Events.CountAsync(e => !dbContext.PriceCategories.Any(pc => pc.EventId == e.Id)));
    }

    [Fact]
    public async Task ImportAsync_CreatesNoSeatForAisleColumn()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_CreatesNoSeatForAisleColumn));

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        var kleinerSaal = await dbContext.Rooms.SingleAsync(r => r.Name == "Kleiner Saal");
        var seats = await dbContext.Seats.Where(s => s.RoomId == kleinerSaal.Id).ToListAsync();

        Assert.DoesNotContain(seats, s => s.Row == "A" && s.Column == 6);
        Assert.Contains(seats, s => s.Row == "A" && s.Column == 5);
        Assert.Contains(seats, s => s.Row == "A" && s.Column == 7);
    }

    [Fact]
    public async Task ImportAsync_ImportsBelegteSitzplaetzeAsSingleSharedBooking()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_ImportsBelegteSitzplaetzeAsSingleSharedBooking));

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        var e1 = await dbContext.Events.SingleAsync(e => e.Titel == "Kammerkonzert Frühling");
        var bookings = await dbContext.Bookings.Where(b => b.EventId == e1.Id).ToListAsync();
        Assert.Single(bookings);

        var booking = bookings[0];
        Assert.Equal("Anfangsbestand", booking.Name);
        Assert.Equal("anfangsbestand@import.lokal", booking.Email);
        Assert.Equal(BookingStatus.Active, booking.Status);

        var bookingSeats = await dbContext.BookingSeats
            .Where(bs => bs.BookingId == booking.Id && bs.IsActive)
            .Include(bs => bs.Seat)
            .ToListAsync();
        Assert.Equal(3, bookingSeats.Count);
        Assert.Equal(
            new HashSet<string> { "B3", "B4", "C7" },
            bookingSeats.Select(bs => bs.Seat!.Row + bs.Seat!.Column).ToHashSet());

        var firstPriceCategoryId = await dbContext.PriceCategories
            .Where(pc => pc.EventId == e1.Id)
            .OrderBy(pc => pc.Reihenfolge)
            .Select(pc => pc.Id)
            .FirstAsync();
        Assert.All(bookingSeats, bs => Assert.Equal(firstPriceCategoryId, bs.PriceCategoryId));
    }

    [Fact]
    public async Task ImportAsync_EventWithoutBelegteSitzplaetze_CreatesNoBooking()
    {
        using var dbContext = CreateContext(nameof(ImportAsync_EventWithoutBelegteSitzplaetze_CreatesNoBooking));

        await SeedImporter.ImportAsync(dbContext, new BookingService(dbContext), AppContext.BaseDirectory);

        var e2 = await dbContext.Events.SingleAsync(e => e.Titel == "Sinfonisches Openair-Programm");
        var bookingCount = await dbContext.Bookings.CountAsync(b => b.EventId == e2.Id);
        Assert.Equal(0, bookingCount);
    }
}
