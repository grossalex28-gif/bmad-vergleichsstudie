using Microsoft.Data.Sqlite;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _db = TestDbContextFactory.CreateSqliteInMemory(out _connection);
        _sut = new OrderService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private (Product Product1, Product Product2, Supplier Supplier1, Supplier Supplier2) SeedFixture()
    {
        var category = new Category { Id = "K1", Name = "Elektronik" };
        var subcategory = new Subcategory { Id = "K1a", Name = "Kopfhörer", Category = category };
        _db.Categories.Add(category);
        _db.Subcategories.Add(subcategory);

        var product1 = new Product
        {
            Id = "P1",
            Name = "Ohrhörer Modell Compact",
            Description = "Kompakte In-Ear-Kopfhörer",
            Subcategory = subcategory,
        };
        var product2 = new Product
        {
            Id = "P2",
            Name = "Kopfhörer Modell Studio",
            Description = "Over-Ear-Kopfhörer",
            Subcategory = subcategory,
        };
        _db.Products.AddRange(product1, product2);

        var supplier1 = new Supplier { Id = "S1", Name = "Lieferant 1" };
        var supplier2 = new Supplier { Id = "S2", Name = "Lieferant 2" };
        _db.Suppliers.AddRange(supplier1, supplier2);

        _db.Offers.AddRange(
            new Offer { ProductId = product1.Id, SupplierId = supplier1.Id, Price = 27.50m },
            new Offer { ProductId = product1.Id, SupplierId = supplier2.Id, Price = 29.90m },
            new Offer { ProductId = product2.Id, SupplierId = supplier1.Id, Price = 99.00m });

        _db.SaveChanges();

        return (product1, product2, supplier1, supplier2);
    }

    private static OrderCreateDto ValidRequest(params OrderItemCreateDto[] items) => new()
    {
        Items = [.. items],
        Delivery = new DeliveryDto
        {
            Name = "Mira Muster",
            Street = "Musterstraße 1",
            PostalCode = "12345",
            City = "Musterstadt",
            Country = "Deutschland",
            Email = "mira@example.com",
        },
    };

    [Fact]
    public async Task CreateOrderAsync_ValidRequest_CreatesOrderAndOrderItemWithServerSidePrice()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 2 });

        var result = await _sut.CreateOrderAsync(request);

        Assert.True(result.Success);
        var order = Assert.Single(_db.Orders);
        Assert.NotEqual(Guid.Empty, order.PublicId);
        var orderItem = Assert.Single(_db.OrderItems);
        Assert.Equal(27.50m, orderItem.UnitPriceAtOrder);
        Assert.Equal(2, orderItem.Quantity);

        Assert.NotNull(result.Order);
        Assert.Equal("Received", result.Order!.Status);
        Assert.Equal(55.00m, result.Order.TotalAmount);
        Assert.Equal("Mira Muster", result.Order.Delivery.Name);
        Assert.Equal("Musterstraße 1", result.Order.Delivery.Street);
        Assert.Equal("12345", result.Order.Delivery.PostalCode);
        Assert.Equal("Musterstadt", result.Order.Delivery.City);
        Assert.Equal("Deutschland", result.Order.Delivery.Country);
        Assert.Equal("mira@example.com", result.Order.Delivery.Email);
    }

    [Fact]
    public async Task CreateOrderAsync_OfferPriceChangesAfterOrder_UnitPriceAtOrderStaysUnchanged()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 1 });
        await _sut.CreateOrderAsync(request);

        var offer = _db.Offers.Single(o => o.ProductId == fixture.Product1.Id && o.SupplierId == fixture.Supplier1.Id);
        offer.Price = 999.99m;
        _db.SaveChanges();

        var orderItem = Assert.Single(_db.OrderItems);
        Assert.Equal(27.50m, orderItem.UnitPriceAtOrder);
    }

    [Fact]
    public async Task CreateOrderAsync_EmptyItems_ReturnsInvalidAndPersistsNothing()
    {
        SeedFixture();
        var request = ValidRequest();

        var result = await _sut.CreateOrderAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_db.Orders);
        Assert.Empty(_db.OrderItems);
        Assert.True(result.InvalidLines is null or { Count: 0 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateOrderAsync_InvalidQuantityAmongMultipleLines_RejectsWholeOrderAtomically(int invalidQuantity)
    {
        var fixture = SeedFixture();
        var request = ValidRequest(
            new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 1 },
            new OrderItemCreateDto { ProductId = fixture.Product2.Id, SupplierId = fixture.Supplier1.Id, Quantity = invalidQuantity });

        var result = await _sut.CreateOrderAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_db.Orders);
        Assert.Empty(_db.OrderItems);
        var invalidLine = Assert.Single(result.InvalidLines!);
        Assert.Equal(fixture.Product2.Id, invalidLine.ProductId);
        Assert.Equal(fixture.Supplier1.Id, invalidLine.SupplierId);
        Assert.Equal(OrderRejectionReasons.InvalidQuantity, invalidLine.Reason);
        Assert.DoesNotContain(result.InvalidLines!, l => l.ProductId == fixture.Product1.Id);
    }

    [Fact]
    public async Task CreateOrderAsync_OfferDoesNotExistForProductAndSupplier_ReturnsInvalidAndPersistsNothing()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(new OrderItemCreateDto { ProductId = fixture.Product2.Id, SupplierId = fixture.Supplier2.Id, Quantity = 1 });

        var result = await _sut.CreateOrderAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_db.Orders);
        Assert.Empty(_db.OrderItems);
        var invalidLine = Assert.Single(result.InvalidLines!);
        Assert.Equal(fixture.Product2.Id, invalidLine.ProductId);
        Assert.Equal(fixture.Supplier2.Id, invalidLine.SupplierId);
        Assert.Equal(OrderRejectionReasons.OfferNotFound, invalidLine.Reason);
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleInvalidLinesWithDifferentReasons_CollectsAllInvalidLines()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(
            new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 0 },
            new OrderItemCreateDto { ProductId = fixture.Product2.Id, SupplierId = fixture.Supplier2.Id, Quantity = 1 });

        var result = await _sut.CreateOrderAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_db.Orders);
        Assert.Empty(_db.OrderItems);
        Assert.Equal(2, result.InvalidLines!.Count);
        Assert.Contains(result.InvalidLines!, l =>
            l.ProductId == fixture.Product1.Id && l.SupplierId == fixture.Supplier1.Id && l.Reason == OrderRejectionReasons.InvalidQuantity);
        Assert.Contains(result.InvalidLines!, l =>
            l.ProductId == fixture.Product2.Id && l.SupplierId == fixture.Supplier2.Id && l.Reason == OrderRejectionReasons.OfferNotFound);
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleValidLines_CreatesAllOrderItemsWithCorrectTotalAmount()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(
            new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 2 },
            new OrderItemCreateDto { ProductId = fixture.Product2.Id, SupplierId = fixture.Supplier1.Id, Quantity = 1 });

        var result = await _sut.CreateOrderAsync(request);

        Assert.True(result.Success);
        Assert.Equal(2, _db.OrderItems.Count());
        Assert.Equal(2 * 27.50m + 99.00m, result.Order!.TotalAmount);
    }

    [Fact]
    public async Task GetOrderByPublicIdAsync_ExistingOrder_ReturnsOrderDtoWithItemsAndTotal()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(
            new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 2 },
            new OrderItemCreateDto { ProductId = fixture.Product2.Id, SupplierId = fixture.Supplier1.Id, Quantity = 1 });
        var result = await _sut.CreateOrderAsync(request);

        var order = await _sut.GetOrderByPublicIdAsync(result.Order!.PublicId);

        Assert.NotNull(order);
        Assert.Equal(result.Order.PublicId, order!.PublicId);
        Assert.Equal(result.Order.CreatedAt, order.CreatedAt);
        Assert.Equal("Received", order.Status);
        Assert.Equal("Mira Muster", order.Delivery.Name);
        Assert.Equal("Musterstraße 1", order.Delivery.Street);
        Assert.Equal("12345", order.Delivery.PostalCode);
        Assert.Equal("Musterstadt", order.Delivery.City);
        Assert.Equal("Deutschland", order.Delivery.Country);
        Assert.Equal("mira@example.com", order.Delivery.Email);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(fixture.Product1.Id, order.Items[0].ProductId);
        Assert.Equal(fixture.Product2.Id, order.Items[1].ProductId);
        var item1 = order.Items.Single(i => i.ProductId == fixture.Product1.Id);
        Assert.Equal("Ohrhörer Modell Compact", item1.ProductName);
        Assert.Equal("Lieferant 1", item1.SupplierName);
        Assert.Equal(27.50m, item1.UnitPrice);
        Assert.Equal(2, item1.Quantity);
        Assert.Equal(55.00m, item1.LineTotal);
        var item2 = order.Items.Single(i => i.ProductId == fixture.Product2.Id);
        Assert.Equal("Kopfhörer Modell Studio", item2.ProductName);
        Assert.Equal("Lieferant 1", item2.SupplierName);
        Assert.Equal(99.00m, item2.UnitPrice);
        Assert.Equal(1, item2.Quantity);
        Assert.Equal(99.00m, item2.LineTotal);
        Assert.Equal(2 * 27.50m + 99.00m, order.TotalAmount);
    }

    [Fact]
    public async Task GetOrderByPublicIdAsync_OfferPriceChangesAfterOrder_ReturnsSnapshotPrice()
    {
        var fixture = SeedFixture();
        var request = ValidRequest(new OrderItemCreateDto { ProductId = fixture.Product1.Id, SupplierId = fixture.Supplier1.Id, Quantity = 1 });
        var result = await _sut.CreateOrderAsync(request);

        var offer = _db.Offers.Single(o => o.ProductId == fixture.Product1.Id && o.SupplierId == fixture.Supplier1.Id);
        offer.Price = 999.99m;
        _db.SaveChanges();

        var order = await _sut.GetOrderByPublicIdAsync(result.Order!.PublicId);

        Assert.NotNull(order);
        Assert.Equal(27.50m, order!.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task GetOrderByPublicIdAsync_UnknownPublicId_ReturnsNull()
    {
        SeedFixture();

        var order = await _sut.GetOrderByPublicIdAsync(Guid.NewGuid());

        Assert.Null(order);
    }
}
