using Microsoft.Data.Sqlite;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests;

public class CategoryQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly CategoryQueryService _sut;

    public CategoryQueryServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new CategoryQueryService(_db);

        var categoryElektronik = new Category { Id = "K1", Name = "Elektronik" };
        var categoryHaushalt = new Category { Id = "K2", Name = "Haushalt" };
        _db.Categories.AddRange(categoryElektronik, categoryHaushalt);

        _db.Subcategories.AddRange(
            new Subcategory { Id = "K1a", Name = "Kopfhörer", Category = categoryElektronik },
            new Subcategory { Id = "K1b", Name = "Smartphones & Zubehör", Category = categoryElektronik },
            new Subcategory { Id = "K2a", Name = "Küchengeräte", Category = categoryHaushalt });

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategoriesWithTheirSubcategories()
    {
        var result = await _sut.GetCategoriesAsync();

        Assert.Equal(2, result.Count);

        var elektronik = result.Single(c => c.Id == "K1");
        Assert.Equal("Elektronik", elektronik.Name);
        Assert.Equal(2, elektronik.Subcategories.Count);
        Assert.Contains(elektronik.Subcategories, s => s.Id == "K1a" && s.Name == "Kopfhörer");
        Assert.Contains(elektronik.Subcategories, s => s.Id == "K1b" && s.Name == "Smartphones & Zubehör");

        var haushalt = result.Single(c => c.Id == "K2");
        Assert.Single(haushalt.Subcategories);
        Assert.Equal("K2a", haushalt.Subcategories[0].Id);
    }

    [Fact]
    public async Task GetCategoriesAsync_CategoryWithoutSubcategories_ReturnsEmptySubcategoryList()
    {
        _db.Categories.Add(new Category { Id = "K3", Name = "Leer" });
        _db.SaveChanges();

        var result = await _sut.GetCategoriesAsync();

        var leer = result.Single(c => c.Id == "K3");
        Assert.Empty(leer.Subcategories);
    }
}
