using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests.Services;

public class OrderServiceTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedOfferAsync(AppDbContext db, string productId = "P1", string supplierId = "L1", decimal price = 10m)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == "C1"))
        {
            db.Categories.Add(new Category { Id = "C1", Name = "Kategorie 1" });
        }
        if (!await db.Subcategories.AnyAsync(s => s.Id == "S1"))
        {
            db.Subcategories.Add(new Subcategory { Id = "S1", Name = "Unterkategorie 1", CategoryId = "C1" });
        }
        if (!await db.Suppliers.AnyAsync(s => s.Id == supplierId))
        {
            db.Suppliers.Add(new Supplier { Id = supplierId, Name = $"Lieferant {supplierId}" });
        }
        if (!await db.Products.AnyAsync(p => p.Id == productId))
        {
            db.Products.Add(new Product { Id = productId, Name = $"Produkt {productId}", Description = "d", SubcategoryId = "S1", Attributes = "{}" });
        }
        db.Offers.Add(new Offer { ProductId = productId, SupplierId = supplierId, Price = price });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateOrderAsync_ValidCart_SnapshotsSupplierNameAndCurrentPriceAndClearsNoTrace()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 19.99m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 2)]);

        var result = await service.CreateOrderAsync(request);

        Assert.NotNull(result.OrderId);
        var order = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == result.OrderId);
        Assert.Equal("Eingegangen", order.Status);
        var item = Assert.Single(order.Items);
        Assert.Equal("Lieferant L1", item.SupplierName);
        Assert.Equal(19.99m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task CreateOrderAsync_SupplierChangesPriceAfterOrder_OrderItemPriceStaysFrozen()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 19.99m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1)]);
        var result = await service.CreateOrderAsync(request);

        var offer = await db.Offers.SingleAsync(o => o.ProductId == "P1" && o.SupplierId == "L1");
        offer.Price = 29.99m;
        await db.SaveChangesAsync();

        var item = await db.OrderItems.SingleAsync(i => i.OrderId == result.OrderId);
        Assert.Equal(19.99m, item.UnitPrice);
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyItemsList_RejectsWithEmptyCartAndPersistsNothing()
    {
        var db = CreateContext();
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com", []);

        var result = await service.CreateOrderAsync(request);

        Assert.Null(result.OrderId);
        Assert.Equal("empty-cart", result.RejectionReason);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItems.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_NullItems_RejectsWithEmptyCartWithoutThrowing()
    {
        var db = CreateContext();
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com", null!);

        var result = await service.CreateOrderAsync(request);

        Assert.Equal("empty-cart", result.RejectionReason);
    }

    [Fact]
    public async Task CreateOrderAsync_NullItemAmongValidItems_RejectsWithInvalidQuantityWithoutThrowing()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 10m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1), null!]);

        var result = await service.CreateOrderAsync(request);

        Assert.Equal("invalid-quantity", result.RejectionReason);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItems.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_OneInvalidQuantityAmongValidItems_RejectsWholeRequestAndPersistsNothing()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 10m);
        await SeedOfferAsync(db, "P2", "L1", 20m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1), new OrderLineRequestDto("P2", "L1", 0)]);

        var result = await service.CreateOrderAsync(request);

        Assert.Equal("invalid-quantity", result.RejectionReason);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItems.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_OfferNoLongerExistsAmongValidItems_RejectsWholeRequestAndPersistsNothing()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 10m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1), new OrderLineRequestDto("P9", "L9", 1)]);

        var result = await service.CreateOrderAsync(request);

        Assert.Equal("offer-unavailable", result.RejectionReason);
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.OrderItems.CountAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_SameProductDifferentSuppliers_CreatesSeparateOrderItemsWithCorrectPricePerSupplier()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 10m);
        await SeedOfferAsync(db, "P1", "L2", 15m);
        var service = new OrderService(db);
        var request = new CreateOrderRequestDto("Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1), new OrderLineRequestDto("P1", "L2", 3)]);

        var result = await service.CreateOrderAsync(request);

        var items = await db.OrderItems.Where(i => i.OrderId == result.OrderId).ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.SupplierId == "L1" && i.UnitPrice == 10m && i.Quantity == 1);
        Assert.Contains(items, i => i.SupplierId == "L2" && i.UnitPrice == 15m && i.Quantity == 3);
    }

    [Fact]
    public async Task GetOrderAsync_ExistingOrder_ReturnsStatusItemsAndCorrectTotal()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 10m);
        await SeedOfferAsync(db, "P2", "L1", 5m);
        var service = new OrderService(db);
        var created = await service.CreateOrderAsync(new CreateOrderRequestDto(
            "Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 2), new OrderLineRequestDto("P2", "L1", 3)]));

        var result = await service.GetOrderAsync(created.OrderId!.Value);

        Assert.NotNull(result);
        Assert.Equal("Eingegangen", result!.Status);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, i => i.SupplierName == "Lieferant L1" && i.UnitPrice == 10m && i.Quantity == 2);
        Assert.Contains(result.Items, i => i.SupplierName == "Lieferant L1" && i.UnitPrice == 5m && i.Quantity == 3);
        Assert.Equal(35m, result.TotalAmount); // 10*2 + 5*3
    }

    [Fact]
    public async Task GetOrderAsync_OrderExistsButSupplierChangedPriceAfterward_ReturnsFrozenPriceNotCurrentOne()
    {
        var db = CreateContext();
        await SeedOfferAsync(db, "P1", "L1", 19.99m);
        var service = new OrderService(db);
        var created = await service.CreateOrderAsync(new CreateOrderRequestDto(
            "Jonas", "Hauptstr. 1", "12345", "Berlin", "DE", "j@example.com",
            [new OrderLineRequestDto("P1", "L1", 1)]));

        var offer = await db.Offers.SingleAsync(o => o.ProductId == "P1" && o.SupplierId == "L1");
        offer.Price = 29.99m;
        await db.SaveChangesAsync();

        var result = await service.GetOrderAsync(created.OrderId!.Value);

        Assert.Equal(19.99m, Assert.Single(result!.Items).UnitPrice);
        Assert.Equal(19.99m, result.TotalAmount);
    }

    [Fact]
    public async Task GetOrderAsync_UnknownId_ReturnsNull()
    {
        var db = CreateContext();
        var service = new OrderService(db);

        var result = await service.GetOrderAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
