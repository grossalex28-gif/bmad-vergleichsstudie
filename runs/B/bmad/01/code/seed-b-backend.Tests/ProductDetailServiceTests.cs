using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests;

public class ProductDetailServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ProductDetailService _sut;

    public ProductDetailServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new ProductDetailService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private (Product product, Product productWithRatings) SeedFixture()
    {
        var category = new Category { Id = "K1", Name = "Elektronik" };
        var subcategory = new Subcategory { Id = "K1a", Name = "Kopfhörer", Category = category };
        _db.Categories.Add(category);
        _db.Subcategories.Add(subcategory);

        var farbe = new PropertyDefinition { SubcategoryId = subcategory.Id, Name = "Farbe" };
        var material = new PropertyDefinition { SubcategoryId = subcategory.Id, Name = "Material" };
        _db.PropertyDefinitions.AddRange(material, farbe);

        var supplier1 = new Supplier { Id = "S1", Name = "Lieferant 1" };
        var supplier2 = new Supplier { Id = "S2", Name = "Lieferant 2" };
        _db.Suppliers.AddRange(supplier2, supplier1);

        var product = new Product
        {
            Id = "P1",
            Name = "Ohrhörer Modell Compact",
            Description = "Kompakte In-Ear-Kopfhörer",
            Subcategory = subcategory,
        };
        var productWithRatings = new Product
        {
            Id = "P2",
            Name = "Bügelkopfhörer Studio",
            Description = "Over-Ear-Kopfhörer",
            Subcategory = subcategory,
        };
        _db.Products.AddRange(product, productWithRatings);
        _db.SaveChanges();

        _db.ProductProperties.AddRange(
            new ProductProperty { ProductId = product.Id, PropertyDefinition = farbe, Value = "Schwarz" },
            new ProductProperty { ProductId = product.Id, PropertyDefinition = material, Value = "Kunststoff" });

        _db.Offers.AddRange(
            new Offer { ProductId = product.Id, SupplierId = supplier2.Id, Price = 29.90m },
            new Offer { ProductId = product.Id, SupplierId = supplier1.Id, Price = 27.50m });

        _db.Ratings.AddRange(
            new Rating { ProductId = productWithRatings.Id, AuthorName = "Anna", Value = 4 },
            new Rating { ProductId = productWithRatings.Id, AuthorName = "Ben", Value = 2 });

        _db.SaveChanges();

        return (product, productWithRatings);
    }

    [Fact]
    public async Task GetProductDetailAsync_ExistingProduct_ReturnsNameDescriptionCategoryPropertiesAndAllOffersSorted()
    {
        var (product, _) = SeedFixture();

        var result = await _sut.GetProductDetailAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Ohrhörer Modell Compact", result!.Name);
        Assert.Equal("Kompakte In-Ear-Kopfhörer", result.Description);
        Assert.Equal("K1", result.CategoryId);
        Assert.Equal("Elektronik", result.CategoryName);
        Assert.Equal("K1a", result.SubcategoryId);
        Assert.Equal("Kopfhörer", result.SubcategoryName);

        Assert.Equal(new[] { "Farbe", "Material" }, result.Properties.Select(p => p.Name));
        Assert.Equal("Schwarz", result.Properties.Single(p => p.Name == "Farbe").Value);

        Assert.Equal(new[] { "S1", "S2" }, result.Offers.Select(o => o.SupplierId));
        Assert.Equal("Lieferant 1", result.Offers.Single(o => o.SupplierId == "S1").SupplierName);
        Assert.Equal(27.50m, result.Offers.Single(o => o.SupplierId == "S1").Price);
        Assert.Equal(29.90m, result.Offers.Single(o => o.SupplierId == "S2").Price);
    }

    [Fact]
    public async Task GetProductDetailAsync_ProductWithoutRatings_ReturnsNullAverageAndZeroCount()
    {
        var (product, _) = SeedFixture();

        var result = await _sut.GetProductDetailAsync(product.Id);

        Assert.NotNull(result);
        Assert.Null(result!.AverageRating);
        Assert.Equal(0, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_ProductWithRatings_ReturnsCorrectAverageAndCount()
    {
        var (_, productWithRatings) = SeedFixture();

        var result = await _sut.GetProductDetailAsync(productWithRatings.Id);

        Assert.NotNull(result);
        Assert.Equal(3m, result!.AverageRating);
        Assert.Equal(2, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_RatingsWithFractionalAverage_ReturnsPreciseAverage()
    {
        var (_, productWithRatings) = SeedFixture();
        _db.Ratings.Add(new Rating { ProductId = productWithRatings.Id, AuthorName = "Cara", Value = 5 });
        _db.SaveChanges();

        var result = await _sut.GetProductDetailAsync(productWithRatings.Id);

        Assert.NotNull(result);
        Assert.Equal(11m / 3m, result!.AverageRating);
        Assert.Equal(3, result.RatingCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_UnknownId_ReturnsNull()
    {
        SeedFixture();

        var result = await _sut.GetProductDetailAsync("unbekannt");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductDetailAsync_CalledTwice_IncrementsPersistedViewCountToTwo()
    {
        var (product, _) = SeedFixture();

        await _sut.GetProductDetailAsync(product.Id);
        await _sut.GetProductDetailAsync(product.Id);

        var stored = await _db.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        Assert.Equal(2, stored.ViewCount);
    }

    [Fact]
    public async Task GetProductDetailAsync_UnknownId_DoesNotChangeViewCountOfOtherProducts()
    {
        var (product, _) = SeedFixture();

        await _sut.GetProductDetailAsync("unbekannt");

        var stored = await _db.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        Assert.Equal(0, stored.ViewCount);
    }
}
