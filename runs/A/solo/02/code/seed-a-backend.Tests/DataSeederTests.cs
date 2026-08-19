using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

public class DataSeederTests : IDisposable
{
    private readonly SqliteDbContextFixture _fixture = new();

    private static string SeedFilePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "seed-a-backend.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Konnte das Repository-Wurzelverzeichnis nicht finden.");
        }

        return Path.Combine(directory.FullName, "seed-a-backend.Api", "Data", "anfangsdatenbestand.json");
    }

    [Fact]
    public async Task SeedIfEmptyAsync_PopulatesVenuesRoomsEventsAndPriceCategories()
    {
        await using var db = _fixture.CreateContext();

        await DataSeeder.SeedIfEmptyAsync(db, SeedFilePath());

        Assert.Equal(2, db.Venues.Count());
        Assert.Equal(4, db.Rooms.Count());
        Assert.Equal(6, db.Events.Count());
        Assert.Equal(12, db.PriceCategories.Count());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_CreatesPreOccupiedSeatsFromInitialData()
    {
        await using var db = _fixture.CreateContext();

        await DataSeeder.SeedIfEmptyAsync(db, SeedFilePath());

        var seededBooking = db.Bookings.Single(b => b.EventId == "E1");
        Assert.Equal(3, db.BookingSeats.Count(s => s.BookingId == seededBooking.Id));
    }

    [Fact]
    public async Task SeedIfEmptyAsync_DoesNothingWhenVenuesAlreadyExist()
    {
        await using var db = _fixture.CreateContext();
        await TestData.SeedSmallEventAsync(db);

        await DataSeeder.SeedIfEmptyAsync(db, SeedFilePath());

        Assert.Single(db.Venues);
    }

    public void Dispose() => _fixture.Dispose();
}
