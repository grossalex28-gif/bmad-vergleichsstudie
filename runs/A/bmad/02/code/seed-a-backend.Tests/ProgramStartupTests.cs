using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

/// <summary>
/// Testet die tatsächliche Startup-Sequenz aus `Program.cs` (DbContext-Registrierung,
/// `MigrateAsync()`, `!db.Venues.Any()`-Gate) über `WebApplicationFactory`, statt sich auf
/// die bisher rein manuelle Verifikation aus den Dev Notes zu verlassen.
/// </summary>
public class ProgramStartupTests : IAsyncLifetime
{
    private readonly string _connectionString = SqlServerTestDatabase.CreateIsolatedConnectionString();

    private static readonly string SeedFixturePath = Path.Combine(AppContext.BaseDirectory, "TestData", "seed-fixture.json");

    // Anders als SqlServerTestDatabase.InitializeAsync() migriert dieser Setup-Schritt bewusst
    // NICHT vorab — das Anlegen des Schemas beim ersten App-Start ist selbst Testgegenstand.
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task App_Start_migriert_das_Schema_und_importiert_den_Anfangsdatenbestand_gegen_eine_leere_DB()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath));

        using var client = factory.CreateClient();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);

        Assert.Equal(2, await db.Venues.CountAsync(ct));
        Assert.Equal(3, await db.Rooms.CountAsync(ct));
        Assert.Equal(2, await db.Events.CountAsync(ct));
    }

    [Fact]
    public async Task App_Start_importiert_bei_bereits_befuellter_DB_nicht_erneut()
    {
        var ct = TestContext.Current.CancellationToken;

        await using (var ersterStart = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath)))
        {
            using var client = ersterStart.CreateClient();
        }

        await using (var zweiterStart = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder => webBuilder
                .UseSetting("ConnectionStrings:Default", _connectionString)
                .UseSetting("Seed:FilePath", SeedFixturePath)))
        {
            using var client = zweiterStart.CreateClient();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var db = new AppDbContext(options);
        Assert.Equal(2, await db.Venues.CountAsync(ct));
    }
}
