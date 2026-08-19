using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests.Application;

public class CartServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CartService _service;

    public CartServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _service = new CartService(_context);

        _context.Categories.Add(new Category { Id = "CC", Name = "Warenkorbkategorie" });
        _context.Subcategories.Add(new Subcategory { Id = "SC1", Name = "Warenkorbunterkategorie", CategoryId = "CC" });
        _context.Products.Add(new Product { Id = "PC1", Name = "Warenkorbprodukt", Description = "Beschreibung", SubcategoryId = "SC1" });
        _context.Suppliers.Add(new Supplier { Id = "LC1", Name = "Lieferant Eins" });
        _context.Suppliers.Add(new Supplier { Id = "LC2", Name = "Lieferant Zwei" });
        _context.Offers.Add(new Offer { ProductId = "PC1", SupplierId = "LC1", Price = 10.00m });
        _context.Offers.Add(new Offer { ProductId = "PC1", SupplierId = "LC2", Price = 12.00m });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AddItemAsync_NoCartId_CreatesNewCartAndItem()
    {
        var result = await _service.AddItemAsync(null, "PC1", "LC1", 2);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.CartId);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(10.00m, result.Items[0].UnitPrice);
        Assert.Equal(20.00m, result.TotalPrice);
        Assert.Equal(1, await _context.Carts.CountAsync());
    }

    [Fact]
    public async Task AddItemAsync_SameProductDifferentSupplier_CreatesSeparateLine()
    {
        var first = await _service.AddItemAsync(null, "PC1", "LC1", 1);

        var result = await _service.AddItemAsync(first!.CartId, "PC1", "LC2", 1);

        Assert.Equal(2, result!.Items.Count);
        Assert.Contains(result.Items, l => l.SupplierId == "LC1");
        Assert.Contains(result.Items, l => l.SupplierId == "LC2");
    }

    [Fact]
    public async Task AddItemAsync_SameProductSupplierPairAgain_IncreasesQuantityWithoutDuplicateLine()
    {
        var first = await _service.AddItemAsync(null, "PC1", "LC1", 2);

        var result = await _service.AddItemAsync(first!.CartId, "PC1", "LC1", 3);

        Assert.Single(result!.Items);
        Assert.Equal(5, result.Items[0].Quantity);
        Assert.Equal(1, await _context.CartItems.CountAsync());
    }

    [Fact]
    public async Task AddItemAsync_UnknownOffer_ReturnsNullAndPersistsNothing()
    {
        var result = await _service.AddItemAsync(null, "PC1", "UNBEKANNT", 1);

        Assert.Null(result);
        Assert.Equal(0, await _context.Carts.CountAsync());
        Assert.Equal(0, await _context.CartItems.CountAsync());
    }

    [Fact]
    public async Task AddItemAsync_UnknownExistingCartId_CreatesNewCartWithDifferentId()
    {
        var unknownCartId = Guid.NewGuid();

        var result = await _service.AddItemAsync(unknownCartId, "PC1", "LC1", 1);

        Assert.NotNull(result);
        Assert.NotEqual(unknownCartId, result!.CartId);
        Assert.Equal(1, await _context.Carts.CountAsync());
    }

    [Fact]
    public async Task GetCartAsync_NoCartId_ReturnsEmptyCartWithoutError()
    {
        var result = await _service.GetCartAsync(null);

        Assert.Null(result.CartId);
        Assert.Empty(result.Items);
        Assert.Equal(0m, result.TotalPrice);
    }

    [Fact]
    public async Task GetCartAsync_UnknownCartId_ReturnsEmptyCartWithoutError()
    {
        var result = await _service.GetCartAsync(Guid.NewGuid());

        Assert.Null(result.CartId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCartAsync_WithItems_ReturnsNamesPricesAndTotals()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 3);

        var result = await _service.GetCartAsync(added!.CartId);

        Assert.Equal(added.CartId, result.CartId);
        Assert.Single(result.Items);
        Assert.Equal("Warenkorbprodukt", result.Items[0].ProductName);
        Assert.Equal("Lieferant Eins", result.Items[0].SupplierName);
        Assert.Equal(30.00m, result.TotalPrice);
    }

    [Fact]
    public async Task GetCartAsync_ItemWithRemovedOffer_ExcludesLineFromResult()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 1);
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "PC1" && o.SupplierId == "LC1");
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();

        var result = await _service.GetCartAsync(added!.CartId);

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.TotalPrice);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ValidQuantity_UpdatesAndReturnsRecalculatedCart()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 1);

        var result = await _service.UpdateItemQuantityAsync(added!.CartId!.Value, "PC1", "LC1", 5);

        Assert.Equal(CartMutationStatus.Success, result.Status);
        Assert.Equal(5, result.Cart!.Items[0].Quantity);
        Assert.Equal(50.00m, result.Cart.TotalPrice);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_UnknownItem_ReturnsItemNotFound()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 1);

        var result = await _service.UpdateItemQuantityAsync(added!.CartId!.Value, "PC1", "LC2", 2);

        Assert.Equal(CartMutationStatus.ItemNotFound, result.Status);
        Assert.Null(result.Cart);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_OfferNoLongerExists_ReturnsOfferGoneWithoutChangingQuantity()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 2);
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "PC1" && o.SupplierId == "LC1");
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateItemQuantityAsync(added!.CartId!.Value, "PC1", "LC1", 9);

        Assert.Equal(CartMutationStatus.OfferGone, result.Status);
        var stillTwo = await _context.CartItems.SingleAsync(ci => ci.CartId == added.CartId);
        Assert.Equal(2, stillTwo.Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesRowAndRecalculatesTotal()
    {
        var added = await _service.AddItemAsync(null, "PC1", "LC1", 1);
        await _service.AddItemAsync(added!.CartId, "PC1", "LC2", 1);

        var result = await _service.RemoveItemAsync(added.CartId, "PC1", "LC1");

        Assert.Single(result.Items);
        Assert.Equal("LC2", result.Items[0].SupplierId);
        Assert.Equal(0, await _context.CartItems.CountAsync(ci => ci.ProductId == "PC1" && ci.SupplierId == "LC1"));
    }

    [Fact]
    public async Task RemoveItemAsync_UnknownCartOrItem_ReturnsCurrentStateWithoutError()
    {
        var result = await _service.RemoveItemAsync(Guid.NewGuid(), "PC1", "LC1");

        Assert.Null(result.CartId);
        Assert.Empty(result.Items);
    }
}
