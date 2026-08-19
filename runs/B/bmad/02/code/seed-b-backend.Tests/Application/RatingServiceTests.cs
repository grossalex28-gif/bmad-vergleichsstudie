using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests.Application;

public class RatingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly RatingService _service;

    public RatingServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _service = new RatingService(_context);

        _context.Categories.Add(new Category { Id = "CR", Name = "Ratingkategorie" });
        _context.Subcategories.Add(new Subcategory { Id = "SR1", Name = "Ratingunterkategorie", CategoryId = "CR" });
        _context.Products.Add(new Product { Id = "PR1", Name = "Ratingprodukt", Description = "Beschreibung", SubcategoryId = "SR1" });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SubmitRatingAsync_NewRating_CreatesRowAndReturnsAggregate()
    {
        var result = await _service.SubmitRatingAsync("PR1", "Anna", 4);

        Assert.NotNull(result);
        Assert.Equal(4.0, result!.AverageRating);
        Assert.Equal(1, result.RatingCount);
        Assert.Equal(1, await _context.Ratings.CountAsync());
    }

    [Fact]
    public async Task SubmitRatingAsync_SameAuthorSubmitsAgain_ReplacesPreviousRatingWithoutDuplicate()
    {
        await _service.SubmitRatingAsync("PR1", "Anna", 2);

        var result = await _service.SubmitRatingAsync("PR1", "Anna", 5);

        Assert.Equal(1, await _context.Ratings.CountAsync());
        Assert.Equal(5.0, result!.AverageRating);
        Assert.Equal(1, result.RatingCount);
        Assert.Equal(5, await _context.Ratings.Where(r => r.AuthorName == "Anna").Select(r => r.Score).SingleAsync());
    }

    [Fact]
    public async Task SubmitRatingAsync_DifferentAuthors_BothPersistAndAggregateAverages()
    {
        await _service.SubmitRatingAsync("PR1", "Anna", 4);

        var result = await _service.SubmitRatingAsync("PR1", "Ben", 2);

        Assert.Equal(2, await _context.Ratings.CountAsync());
        Assert.Equal(3.0, result!.AverageRating);
        Assert.Equal(2, result.RatingCount);
    }

    [Fact]
    public async Task SubmitRatingAsync_UnknownProductId_ReturnsNullAndPersistsNothing()
    {
        var result = await _service.SubmitRatingAsync("UNBEKANNT", "Anna", 3);

        Assert.Null(result);
        Assert.Equal(0, await _context.Ratings.CountAsync());
    }
}
