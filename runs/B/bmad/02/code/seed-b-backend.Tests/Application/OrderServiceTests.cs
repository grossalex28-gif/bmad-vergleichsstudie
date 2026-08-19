using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Tests.Application;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly OrderService _service;
    private readonly Guid _cartId = Guid.NewGuid();

    public OrderServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _service = new OrderService(_context);

        _context.Categories.Add(new Category { Id = "OC", Name = "Bestellkategorie" });
        _context.Subcategories.Add(new Subcategory { Id = "OS1", Name = "Bestellunterkategorie", CategoryId = "OC" });
        _context.Products.Add(new Product { Id = "OP1", Name = "Bestellprodukt", Description = "Beschreibung", SubcategoryId = "OS1" });
        _context.Suppliers.Add(new Supplier { Id = "OL1", Name = "Lieferant Eins" });
        _context.Suppliers.Add(new Supplier { Id = "OL2", Name = "Lieferant Zwei" });
        _context.Offers.Add(new Offer { ProductId = "OP1", SupplierId = "OL1", Price = 10.00m });
        _context.Offers.Add(new Offer { ProductId = "OP1", SupplierId = "OL2", Price = 12.00m });
        _context.Carts.Add(new Cart { Id = _cartId });
        _context.CartItems.Add(new CartItem { CartId = _cartId, ProductId = "OP1", SupplierId = "OL1", Quantity = 2 });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidCart_CreatesOrderWithSnapshotPriceStatusNeuAndClearsCart()
    {
        var result = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1, 12345 Musterstadt", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.Success, result.Status);
        Assert.Equal("Neu", result.Order!.Status);
        Assert.Single(result.Order.Items);
        Assert.Equal(10.00m, result.Order.Items[0].UnitPrice);
        Assert.Equal(2, result.Order.Items[0].Quantity);
        Assert.Equal(20.00m, result.Order.TotalPrice);
        Assert.Equal(0, await _context.CartItems.CountAsync(ci => ci.CartId == _cartId));
        Assert.Equal(1, await _context.Orders.CountAsync());
    }

    [Fact]
    public async Task PlaceOrderAsync_PriceChangedAfterOrder_OrderItemKeepsOriginalSnapshotPrice()
    {
        var result = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "OP1" && o.SupplierId == "OL1");
        offer.Price = 999.00m;
        await _context.SaveChangesAsync();

        var persisted = await _context.OrderItems.SingleAsync(oi => oi.OrderId == result.Order!.OrderId);

        Assert.Equal(10.00m, persisted.UnitPrice);
    }

    [Fact]
    public async Task PlaceOrderAsync_NoCartId_ReturnsCartEmptyWithoutCreatingOrder()
    {
        var result = await _service.PlaceOrderAsync(null, "Mara Muster", "Musterstraße 1", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.CartEmpty, result.Status);
        Assert.Equal(0, await _context.Orders.CountAsync());
    }

    [Fact]
    public async Task PlaceOrderAsync_UnknownOrEmptyCartId_ReturnsCartEmptyAndLeavesCartUntouched()
    {
        var result = await _service.PlaceOrderAsync(Guid.NewGuid(), "Mara Muster", "Musterstraße 1", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.CartEmpty, result.Status);
        Assert.Equal(1, await _context.CartItems.CountAsync(ci => ci.CartId == _cartId));
    }

    [Fact]
    public async Task PlaceOrderAsync_LineWithInvalidQuantity_RejectsWholeOrderAndLeavesCartUntouched()
    {
        var item = await _context.CartItems.SingleAsync(ci => ci.CartId == _cartId);
        item.Quantity = 0;
        await _context.SaveChangesAsync();

        var result = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.LinesRejected, result.Status);
        Assert.Single(result.RejectedLines!);
        Assert.Equal(OrderLineRejectionReason.InvalidQuantity, result.RejectedLines![0].Reason);
        Assert.Equal(0, await _context.Orders.CountAsync());
        Assert.Equal(1, await _context.CartItems.CountAsync(ci => ci.CartId == _cartId));
    }

    [Fact]
    public async Task PlaceOrderAsync_OfferRemovedSinceAddedToCart_RejectsWholeOrderAndLeavesCartUntouched()
    {
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "OP1" && o.SupplierId == "OL1");
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();

        var result = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.LinesRejected, result.Status);
        Assert.Single(result.RejectedLines!);
        Assert.Equal(OrderLineRejectionReason.OfferGone, result.RejectedLines![0].Reason);
        Assert.Equal("OP1", result.RejectedLines![0].ProductId);
        Assert.Equal("OL1", result.RejectedLines![0].SupplierId);
        Assert.Equal(0, await _context.Orders.CountAsync());
        Assert.Equal(1, await _context.CartItems.CountAsync(ci => ci.CartId == _cartId));
    }

    [Fact]
    public async Task PlaceOrderAsync_TwoLinesOneOfferGone_RejectsAndNamesOnlyTheAffectedLine()
    {
        _context.CartItems.Add(new CartItem { CartId = _cartId, ProductId = "OP1", SupplierId = "OL2", Quantity = 1 });
        await _context.SaveChangesAsync();
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "OP1" && o.SupplierId == "OL2");
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();

        var result = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");

        Assert.Equal(OrderPlacementStatus.LinesRejected, result.Status);
        Assert.Single(result.RejectedLines!);
        Assert.Equal("OL2", result.RejectedLines![0].SupplierId);
    }

    [Fact]
    public async Task GetOrderAsync_ExistingOrder_ReturnsSameDataAsPlacement()
    {
        var placed = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");

        var fetched = await _service.GetOrderAsync(placed.Order!.OrderId);

        Assert.NotNull(fetched);
        Assert.Equal(placed.Order.OrderId, fetched!.OrderId);
        Assert.Equal(placed.Order.Status, fetched.Status);
        Assert.Equal(placed.Order.TotalPrice, fetched.TotalPrice);
        Assert.Single(fetched.Items);
        Assert.Equal(placed.Order.Items[0].ProductId, fetched.Items[0].ProductId);
        Assert.Equal(placed.Order.Items[0].SupplierId, fetched.Items[0].SupplierId);
        Assert.Equal(placed.Order.Items[0].UnitPrice, fetched.Items[0].UnitPrice);
        Assert.Equal(placed.Order.Items[0].Quantity, fetched.Items[0].Quantity);
    }

    [Fact]
    public async Task GetOrderAsync_PriceChangedAfterOrder_StillReturnsSnapshotPrice()
    {
        var placed = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "OP1" && o.SupplierId == "OL1");
        offer.Price = 999.00m;
        await _context.SaveChangesAsync();

        var fetched = await _service.GetOrderAsync(placed.Order!.OrderId);

        Assert.Equal(10.00m, fetched!.Items[0].UnitPrice);
    }

    [Fact]
    public async Task GetOrderAsync_OfferRemovedAfterOrder_StillReturnsOrderUnaffected()
    {
        var placed = await _service.PlaceOrderAsync(_cartId, "Mara Muster", "Musterstraße 1", "mara@example.com");
        var offer = await _context.Offers.SingleAsync(o => o.ProductId == "OP1" && o.SupplierId == "OL1");
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync();

        var fetched = await _service.GetOrderAsync(placed.Order!.OrderId);

        Assert.NotNull(fetched);
        Assert.Equal(10.00m, fetched!.Items[0].UnitPrice);
    }

    [Fact]
    public async Task GetOrderAsync_UnknownOrderId_ReturnsNull()
    {
        var fetched = await _service.GetOrderAsync(Guid.NewGuid());

        Assert.Null(fetched);
    }
}
