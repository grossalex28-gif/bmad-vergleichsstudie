using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests.Services;

public class ProductServiceTests
{
    // ExecuteUpdateAsync (the atomic ViewCount increment, AC #4/AD-9/NFR-2) is not supported by
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

    [Fact]
    public async Task GetProductDetailAsync_ExistingProduct_ReturnsAllFieldsCorrectlyPopulated()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform"] });
        db.Suppliers.Add(new Supplier { Id = "L1", Name = "Lieferant 1" });
        db.Suppliers.Add(new Supplier { Id = "L2", Name = "Lieferant 2" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "Beschreibung 1", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear"}""" });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L1", Price = 30m });
        db.Offers.Add(new Offer { ProductId = "P1", SupplierId = "L2", Price = 12.5m });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P1");

        Assert.NotNull(result);
        Assert.Equal("P1", result!.Id);
        Assert.Equal("Produkt 1", result.Name);
        Assert.Equal("Beschreibung 1", result.Description);
        Assert.Equal("C1", result.CategoryId);
        Assert.Equal("Kategorie 1", result.CategoryName);
        Assert.Equal("S1", result.SubcategoryId);
        Assert.Equal("Unterkategorie 1", result.SubcategoryName);
        Assert.Equal(new[] { ("Bauform", "In-Ear") }, result.Attributes.Select(a => (a.Name, a.Value)));
        Assert.Equal(new[] { ("L2", "Lieferant 2", 12.5m), ("L1", "Lieferant 1", 30m) }, result.Offers.Select(o => (o.SupplierId, o.SupplierName, o.Price)));
    }

    [Fact]
    public async Task GetProductDetailAsync_ProductWithoutRatings_AverageRatingIsNullAndRatingCountIsZero()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P1");

        Assert.NotNull(result);
        Assert.Null(result!.AverageRating);
        Assert.Equal(0, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_RatingValues544_AverageRatingIs43AndRatingCountIs3()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), ProductId = "P1", AuthorName = "A", Value = 5 });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), ProductId = "P1", AuthorName = "B", Value = 4 });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), ProductId = "P1", AuthorName = "C", Value = 4 });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P1");

        Assert.NotNull(result);
        Assert.Equal(4.3, result!.AverageRating);
        Assert.Equal(3, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_RatingValues53_AverageRatingIs40()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), ProductId = "P1", AuthorName = "A", Value = 5 });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), ProductId = "P1", AuthorName = "B", Value = 3 });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P1");

        Assert.NotNull(result);
        Assert.Equal(4.0, result!.AverageRating);
    }

    [Fact]
    public async Task GetProductDetailAsync_TwoConsecutiveCalls_ViewCountReachesTwoNotOne()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        await service.GetProductDetailAsync("P1");
        await service.GetProductDetailAsync("P1");

        var product = await db.Products.AsNoTracking().SingleAsync(p => p.Id == "P1");
        Assert.Equal(2, product.ViewCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_UnknownId_ReturnsNull()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P9");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductDetailAsync_AttributeWithNullJsonValue_IsExcludedFromAttributes()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1", AttributeNames = ["Bauform", "KapazitaetLiter"] });
        db.Products.Add(new Product { Id = "P10", Name = "Produkt 10", Description = "d", SubcategoryId = "S1", Attributes = """{"Bauform":"In-Ear","KapazitaetLiter":null}""" });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P10");

        Assert.NotNull(result);
        var attributeNames = result!.Attributes.Select(a => a.Name).ToList();
        Assert.Contains("Bauform", attributeNames);
        Assert.DoesNotContain("KapazitaetLiter", attributeNames);
    }

    [Fact]
    public async Task GetProductDetailAsync_NoOffers_ReturnsEmptyOffersListWithoutException()
    {
        await using var testDb = TestDb.Create();
        var db = testDb.Db;
        db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        db.Products.Add(new Product { Id = "P1", Name = "Produkt 1", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        await db.SaveChangesAsync();
        var service = new ProductService(db);

        var result = await service.GetProductDetailAsync("P1");

        Assert.NotNull(result);
        Assert.Empty(result!.Offers);
    }
}
