using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Data.Seeding;

namespace seed_b_backend.Tests.Data.Seeding;

public class DatabaseInitializerTests
{
    private static readonly string SeedFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "anfangsdatenbestand.json");

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_OnEmptyDatabase_LoadsExpectedCounts()
    {
        await using var db = CreateContext();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        Assert.Equal(2, await db.Categories.CountAsync());
        Assert.Equal(4, await db.Subcategories.CountAsync());
        Assert.Equal(4, await db.Suppliers.CountAsync());
        Assert.Equal(14, await db.Products.CountAsync());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_OnEmptyDatabase_PreservesStringIds()
    {
        await using var db = CreateContext();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        Assert.NotNull(await db.Categories.FindAsync("K1"));
        Assert.NotNull(await db.Subcategories.FindAsync("K1a"));
        Assert.NotNull(await db.Suppliers.FindAsync("L1"));
        Assert.NotNull(await db.Products.FindAsync("P1"));
    }

    [Fact]
    public async Task SeedIfEmptyAsync_OnEmptyDatabase_MapsProductAttributesAsRawJson()
    {
        await using var db = CreateContext();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        var p1 = await db.Products.FindAsync("P1");
        Assert.NotNull(p1);
        using var p1Attributes = JsonDocument.Parse(p1.Attributes);
        Assert.Equal("In-Ear", p1Attributes.RootElement.GetProperty("Bauform").GetString());
        Assert.True(p1Attributes.RootElement.GetProperty("Kabellos").GetBoolean());

        var p10 = await db.Products.FindAsync("P10");
        Assert.NotNull(p10);
        using var p10Attributes = JsonDocument.Parse(p10.Attributes);
        Assert.Equal(JsonValueKind.Null, p10Attributes.RootElement.GetProperty("KapazitaetLiter").ValueKind);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_OnEmptyDatabase_CreatesUniqueOffersPerProductSupplierPair()
    {
        await using var db = CreateContext();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        var offers = await db.Offers.ToListAsync();
        var uniqueKeys = offers.Select(o => (o.ProductId, o.SupplierId)).Distinct().Count();

        Assert.Equal(offers.Count, uniqueKeys);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_CalledTwice_DoesNotDuplicateData()
    {
        await using var db = CreateContext();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);
        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        Assert.Equal(2, await db.Categories.CountAsync());
        Assert.Equal(4, await db.Subcategories.CountAsync());
        Assert.Equal(4, await db.Suppliers.CountAsync());
        Assert.Equal(14, await db.Products.CountAsync());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_OnDatabaseWithExistingProduct_DoesNothing()
    {
        await using var db = CreateContext();
        db.Products.Add(new Product
        {
            Id = "EXISTING",
            Name = "Bereits vorhandenes Produkt",
            Description = "vorab angelegt",
            SubcategoryId = "K1a",
            Attributes = "{}"
        });
        await db.SaveChangesAsync();

        await DatabaseInitializer.SeedIfEmptyAsync(db, SeedFilePath);

        Assert.Equal(1, await db.Products.CountAsync());
        Assert.Equal(0, await db.Categories.CountAsync());
    }
}
