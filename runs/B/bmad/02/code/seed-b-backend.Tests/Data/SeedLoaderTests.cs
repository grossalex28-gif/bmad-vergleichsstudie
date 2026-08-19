using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;

namespace seed_b_backend.Tests.Data;

public class SeedLoaderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public SeedLoaderTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task LoadAsync_FreshDatabase_LoadsExpectedRowCounts()
    {
        await SeedLoader.LoadAsync(_context, AppContext.BaseDirectory);

        Assert.Equal(2, await _context.Categories.CountAsync());
        Assert.Equal(4, await _context.Subcategories.CountAsync());
        Assert.Equal(4, await _context.Suppliers.CountAsync());
        Assert.Equal(14, await _context.Products.CountAsync());
        Assert.Equal(8, await _context.SubcategoryProperties.CountAsync());
        Assert.Equal(17, await _context.Offers.CountAsync());

        Assert.False(await _context.ProductPropertyValues
            .AnyAsync(v => v.ProductId == "P10" && v.Name == "KapazitaetLiter"));
        Assert.False(await _context.ProductPropertyValues
            .AnyAsync(v => v.ProductId == "P4" && v.Name == "Speicherkapazitaet"));
        Assert.False(await _context.ProductPropertyValues
            .AnyAsync(v => v.ProductId == "P6" && v.Name == "Speicherkapazitaet"));

        Assert.Equal("true", await _context.ProductPropertyValues
            .Where(v => v.ProductId == "P1" && v.Name == "Kabellos")
            .Select(v => v.Value)
            .SingleAsync());
        Assert.Equal("900", await _context.ProductPropertyValues
            .Where(v => v.ProductId == "P8" && v.Name == "LeistungWatt")
            .Select(v => v.Value)
            .SingleAsync());
        Assert.Equal("1.0", await _context.ProductPropertyValues
            .Where(v => v.ProductId == "P9" && v.Name == "KapazitaetLiter")
            .Select(v => v.Value)
            .SingleAsync());
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_DoesNotDuplicateData()
    {
        await SeedLoader.LoadAsync(_context, AppContext.BaseDirectory);
        await SeedLoader.LoadAsync(_context, AppContext.BaseDirectory);

        Assert.Equal(14, await _context.Products.CountAsync());
        Assert.Equal(8, await _context.SubcategoryProperties.CountAsync());
        Assert.Equal(17, await _context.Offers.CountAsync());
    }
}
