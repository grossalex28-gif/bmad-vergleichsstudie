using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests.Data;

public class RatingConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public RatingConfigurationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

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
    public void SavingSecondRatingWithSameProductAndAuthor_ThrowsOnPrimaryKeyViolation()
    {
        _context.Ratings.Add(new Rating { ProductId = "PR1", AuthorName = "Anna", Score = 5 });
        _context.SaveChanges();

        // Zweiter Insert über eine eigene DbContext-Instanz (gleiche Connection): so greift
        // tatsächlich der DB-Constraint statt nur EF Cores In-Memory-Change-Tracker, der einen
        // Duplikat-Key im selben Context bereits vor dem SaveChanges als InvalidOperationException
        // ablehnen würde.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var secondContext = new AppDbContext(options);
        secondContext.Ratings.Add(new Rating { ProductId = "PR1", AuthorName = "Anna", Score = 1 });

        Assert.Throws<DbUpdateException>(() => secondContext.SaveChanges());
    }

    [Fact]
    public void SavingRatingsWithDifferentAuthorsForSameProduct_Succeeds()
    {
        _context.Ratings.AddRange(
            new Rating { ProductId = "PR1", AuthorName = "Anna", Score = 5 },
            new Rating { ProductId = "PR1", AuthorName = "Ben", Score = 3 }
        );

        _context.SaveChanges();

        Assert.Equal(2, _context.Ratings.Count());
    }
}
