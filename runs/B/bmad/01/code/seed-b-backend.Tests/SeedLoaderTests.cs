using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;

namespace seed_b_backend.Tests;

public class SeedLoaderTests : IDisposable
{
    private const string SeedJson = """
        {
          "kategorien": [
            {
              "id": "K1", "name": "Elektronik",
              "unterkategorien": [
                { "id": "K1a", "name": "Kopfhörer", "eigenschaften": ["Bauform", "Kabellos", "LautstaerkeDb"] }
              ]
            }
          ],
          "lieferanten": [
            { "id": "L1", "name": "NordTech Distribution" }
          ],
          "produkte": [
            { "id": "P1", "name": "Ohrhörer", "beschreibung": "Kompakte In-Ear-Kopfhörer.",
              "unterkategorieId": "K1a", "eigenschaften": { "Bauform": "In-Ear", "Kabellos": true, "LautstaerkeDb": 85.5 },
              "angebote": [ { "lieferantId": "L1", "preis": 29.99 } ] },
            { "id": "P2", "name": "Handmixer", "beschreibung": "Mixer.",
              "unterkategorieId": "K1a", "eigenschaften": { "Bauform": null },
              "angebote": [ { "lieferantId": "L1", "preis": 19.90 } ] }
          ]
        }
        """;

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly string _seedFilePath;

    public SeedLoaderTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _seedFilePath = Path.GetTempFileName();
        File.WriteAllText(_seedFilePath, SeedJson);
    }

    public void Dispose()
    {
        File.Delete(_seedFilePath);
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SeedAsync_PopulatesAllEntitiesFromSeedFile()
    {
        await SeedLoader.SeedAsync(_db, _seedFilePath);

        Assert.Equal(1, await _db.Categories.CountAsync());
        Assert.Equal(1, await _db.Subcategories.CountAsync());
        Assert.Equal(3, await _db.PropertyDefinitions.CountAsync());
        Assert.Equal(1, await _db.Suppliers.CountAsync());
        Assert.Equal(2, await _db.Products.CountAsync());
        Assert.Equal(2, await _db.Offers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_CalledTwiceGuardedByProductsAny_DoesNotCreateDuplicates()
    {
        if (!await _db.Products.AnyAsync())
        {
            await SeedLoader.SeedAsync(_db, _seedFilePath);
        }

        var productCountAfterFirstStart = await _db.Products.CountAsync();
        var offerCountAfterFirstStart = await _db.Offers.CountAsync();

        if (!await _db.Products.AnyAsync())
        {
            await SeedLoader.SeedAsync(_db, _seedFilePath);
        }

        Assert.Equal(productCountAfterFirstStart, await _db.Products.CountAsync());
        Assert.Equal(offerCountAfterFirstStart, await _db.Offers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ConvertsBooleanValueToLowercaseCanonicalString()
    {
        await SeedLoader.SeedAsync(_db, _seedFilePath);

        var kabellos = await _db.ProductProperties
            .Include(pp => pp.PropertyDefinition)
            .Where(pp => pp.ProductId == "P1" && pp.PropertyDefinition.Name == "Kabellos")
            .SingleAsync();

        Assert.Equal("true", kabellos.Value);
    }

    [Fact]
    public async Task SeedAsync_ConvertsNumericValueToInvariantCultureString()
    {
        await SeedLoader.SeedAsync(_db, _seedFilePath);

        var lautstaerke = await _db.ProductProperties
            .Include(pp => pp.PropertyDefinition)
            .Where(pp => pp.ProductId == "P1" && pp.PropertyDefinition.Name == "LautstaerkeDb")
            .SingleAsync();

        Assert.Equal("85.5", lautstaerke.Value);
    }

    [Fact]
    public async Task SeedAsync_NullValue_DoesNotCreateProductPropertyRow()
    {
        await SeedLoader.SeedAsync(_db, _seedFilePath);

        var hasBauformForP2 = await _db.ProductProperties
            .Include(pp => pp.PropertyDefinition)
            .AnyAsync(pp => pp.ProductId == "P2" && pp.PropertyDefinition.Name == "Bauform");

        Assert.False(hasBauformForP2);
    }
}
