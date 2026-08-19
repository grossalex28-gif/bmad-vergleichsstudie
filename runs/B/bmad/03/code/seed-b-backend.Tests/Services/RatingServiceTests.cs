using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests.Services;

public class RatingServiceTests
{
    // ExecuteUpdateAsync (the race-safe upsert, AC #1/#3/#4/AD-8) is not supported by
    // EF Core's InMemory provider, unlike other tests in this project — SQLite's ":memory:" mode is
    // used instead because it is a real relational engine that does translate ExecuteUpdate to SQL.
    private sealed class TestDb : IAsyncDisposable
    {
        public AppDbContext Db { get; }
        private readonly SqliteConnection _connection;

        private TestDb(AppDbContext db, SqliteConnection connection)
        {
            Db = db;
            _connection = connection;
        }

        public static TestDb Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            db.Database.EnsureCreated();
            return new TestDb(db, connection);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private static async Task SeedProductAsync(AppDbContext db, string productId = "P1")
    {
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = productId, Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task UpsertRatingAsync_ProductWithoutRatings_ReturnsAverage5AndCount1AndPersistsRating()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        await SeedProductAsync(db);
        var service = new RatingService(db);

        var result = await service.UpsertRatingAsync("P1", "Jonas", 5);

        Assert.NotNull(result);
        Assert.Equal(5.0, result!.AverageRating);
        Assert.Equal(1, result.RatingCount);
        var rating = await db.Ratings.SingleAsync(r => r.ProductId == "P1" && r.AuthorName == "Jonas");
        Assert.Equal(5, rating.Value);
    }

    [Fact]
    public async Task UpsertRatingAsync_ExistingRatingSameAuthor_ReplacesValueWithoutCreatingSecondRow()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        await SeedProductAsync(db);
        var service = new RatingService(db);
        await service.UpsertRatingAsync("P1", "Jonas", 3);

        var result = await service.UpsertRatingAsync("P1", "Jonas", 5);

        Assert.NotNull(result);
        Assert.Equal(1, await db.Ratings.CountAsync(r => r.ProductId == "P1" && r.AuthorName == "Jonas"));
        var rating = await db.Ratings.AsNoTracking().SingleAsync(r => r.ProductId == "P1" && r.AuthorName == "Jonas");
        Assert.Equal(5, rating.Value);
    }

    [Fact]
    public async Task UpsertRatingAsync_TwoDifferentAuthors_CreatesSeparateRowsAndAggregatesBoth()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        await SeedProductAsync(db);
        var service = new RatingService(db);
        await service.UpsertRatingAsync("P1", "A", 5);

        var result = await service.UpsertRatingAsync("P1", "B", 3);

        Assert.NotNull(result);
        Assert.Equal(2, result!.RatingCount);
        Assert.Equal(4.0, result.AverageRating);
    }

    [Fact]
    public async Task UpsertRatingAsync_UnknownProductId_ReturnsNullAndPersistsNoRating()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        await SeedProductAsync(db);
        var service = new RatingService(db);

        var result = await service.UpsertRatingAsync("P9", "Jonas", 5);

        Assert.Null(result);
        Assert.Equal(0, await db.Ratings.CountAsync());
    }

    [Fact]
    public async Task UpsertRatingAsync_ThreeRatingsValues544_ReturnsAverage43()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        await SeedProductAsync(db);
        var service = new RatingService(db);
        await service.UpsertRatingAsync("P1", "A", 5);
        await service.UpsertRatingAsync("P1", "B", 4);

        var result = await service.UpsertRatingAsync("P1", "C", 4);

        Assert.NotNull(result);
        Assert.Equal(4.3, result!.AverageRating);
        Assert.Equal(3, result.RatingCount);
    }
}
