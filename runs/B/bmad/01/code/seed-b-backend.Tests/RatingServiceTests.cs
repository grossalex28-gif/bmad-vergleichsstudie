using Microsoft.Data.Sqlite;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests;

public class RatingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly RatingService _sut;

    public RatingServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new RatingService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Product SeedFixture()
    {
        var category = new Category { Id = "K1", Name = "Elektronik" };
        var subcategory = new Subcategory { Id = "K1a", Name = "Kopfhörer", Category = category };
        _db.Categories.Add(category);
        _db.Subcategories.Add(subcategory);

        var product = new Product
        {
            Id = "P1",
            Name = "Ohrhörer Modell Compact",
            Description = "Kompakte In-Ear-Kopfhörer",
            Subcategory = subcategory,
        };
        _db.Products.Add(product);
        _db.SaveChanges();

        return product;
    }

    [Fact]
    public async Task SubmitRatingAsync_ExistingProduct_CreatesRatingRowWithCorrectFields()
    {
        var product = SeedFixture();

        var result = await _sut.SubmitRatingAsync(product.Id, "Mira", 3);

        Assert.True(result);
        var rating = Assert.Single(_db.Ratings);
        Assert.Equal(product.Id, rating.ProductId);
        Assert.Equal("Mira", rating.AuthorName);
        Assert.Equal(3, rating.Value);
    }

    [Fact]
    public async Task SubmitRatingAsync_SameAuthorTwice_ReplacesPreviousRatingWithExactlyOneRow()
    {
        var product = SeedFixture();

        await _sut.SubmitRatingAsync(product.Id, "Mira", 3);
        await _sut.SubmitRatingAsync(product.Id, "Mira", 5);

        var rating = Assert.Single(_db.Ratings);
        Assert.Equal(product.Id, rating.ProductId);
        Assert.Equal("Mira", rating.AuthorName);
        Assert.Equal(5, rating.Value);
    }

    [Fact]
    public async Task SubmitRatingAsync_UnknownProductId_ReturnsFalseAndCreatesNoRating()
    {
        SeedFixture();

        var result = await _sut.SubmitRatingAsync("unbekannt", "Mira", 3);

        Assert.False(result);
        Assert.Empty(_db.Ratings);
    }
}
