using Microsoft.Data.Sqlite;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests;

public class SubcategoryPropertyQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly SubcategoryPropertyQueryService _sut;

    public SubcategoryPropertyQueryServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new SubcategoryPropertyQueryService(_db);

        var category = new Category { Id = "K1", Name = "Elektronik" };
        var sub1 = new Subcategory { Id = "K1a", Name = "Kopfhörer", Category = category };
        var sub2 = new Subcategory { Id = "K1b", Name = "Smartphones & Zubehör", Category = category };
        _db.Categories.Add(category);
        _db.Subcategories.AddRange(sub1, sub2);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetPropertyFilterOptionsAsync_ReturnsOnlyDefinitionsOfRequestedSubcategory()
    {
        var farbe = new PropertyDefinition { SubcategoryId = "K1a", Name = "Farbe" };
        var kabellos = new PropertyDefinition { SubcategoryId = "K1a", Name = "Kabellos" };
        var speicher = new PropertyDefinition { SubcategoryId = "K1b", Name = "Speicherkapazitaet" };
        _db.PropertyDefinitions.AddRange(farbe, kabellos, speicher);
        _db.SaveChanges();

        var result = await _sut.GetPropertyFilterOptionsAsync("K1a");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Name == "Farbe");
        Assert.Contains(result, o => o.Name == "Kabellos");
        Assert.DoesNotContain(result, o => o.Name == "Speicherkapazitaet");
    }

    [Fact]
    public async Task GetPropertyFilterOptionsAsync_ReturnsDistinctSortedValuesFromExistingProducts()
    {
        var farbe = new PropertyDefinition { SubcategoryId = "K1a", Name = "Farbe" };
        _db.PropertyDefinitions.Add(farbe);
        var product1 = new Product { Id = "P1", Name = "Produkt 1", Description = "Beschreibung", SubcategoryId = "K1a" };
        var product2 = new Product { Id = "P2", Name = "Produkt 2", Description = "Beschreibung", SubcategoryId = "K1a" };
        var product3 = new Product { Id = "P3", Name = "Produkt 3", Description = "Beschreibung", SubcategoryId = "K1a" };
        _db.Products.AddRange(product1, product2, product3);
        _db.SaveChanges();

        _db.ProductProperties.AddRange(
            new ProductProperty { ProductId = "P1", PropertyDefinitionId = farbe.Id, Value = "Rot" },
            new ProductProperty { ProductId = "P2", PropertyDefinitionId = farbe.Id, Value = "Blau" },
            new ProductProperty { ProductId = "P3", PropertyDefinitionId = farbe.Id, Value = "Rot" });
        _db.SaveChanges();

        var result = await _sut.GetPropertyFilterOptionsAsync("K1a");

        var option = result.Single(o => o.Name == "Farbe");
        Assert.Equal(new[] { "Blau", "Rot" }, option.Values);
    }

    [Fact]
    public async Task GetPropertyFilterOptionsAsync_DefinitionWithoutAnyProductValue_ReturnsEmptyValuesList()
    {
        var farbe = new PropertyDefinition { SubcategoryId = "K1a", Name = "Farbe" };
        _db.PropertyDefinitions.Add(farbe);
        _db.SaveChanges();

        var result = await _sut.GetPropertyFilterOptionsAsync("K1a");

        var option = result.Single(o => o.Name == "Farbe");
        Assert.Empty(option.Values);
    }

    [Fact]
    public async Task GetPropertyFilterOptionsAsync_UnknownSubcategoryId_ReturnsEmptyList()
    {
        var result = await _sut.GetPropertyFilterOptionsAsync("does-not-exist");

        Assert.Empty(result);
    }
}
