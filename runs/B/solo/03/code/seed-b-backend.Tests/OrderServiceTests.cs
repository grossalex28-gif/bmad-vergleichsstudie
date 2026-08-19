using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests;

public class OrderServiceTests
{
    private static CreateOrderRequest ValidRequest(params CreateOrderItemRequest[] items) => new(
        "Max Mustermann", "max@example.com", "Musterweg 1", "12345", "Musterstadt", [.. items]);

    [Fact]
    public async Task CreateOrderAsync_EmptyCart_Throws()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        var request = ValidRequest();

        await Assert.ThrowsAsync<OrderValidationException>(() => service.CreateOrderAsync(request));
        Assert.Empty(db.Orders);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateOrderAsync_InvalidQuantity_ThrowsAndPersistsNothing(int quantity)
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        var request = ValidRequest(new CreateOrderItemRequest("P1", "L1", quantity));

        await Assert.ThrowsAsync<OrderValidationException>(() => service.CreateOrderAsync(request));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrderAsync_ProductNotOfferedByChosenSupplier_ThrowsAndPersistsNothing()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        // P2 wird nur von L1 angeboten, nicht von L2.
        var request = ValidRequest(new CreateOrderItemRequest("P2", "L2", 1));

        await Assert.ThrowsAsync<OrderValidationException>(() => service.CreateOrderAsync(request));
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrderAsync_OneOfMultipleItemsInvalid_CreatesNoPartialOrder()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        var request = ValidRequest(
            new CreateOrderItemRequest("P1", "L1", 1),
            new CreateOrderItemRequest("P2", "L2", 1)); // ungültig: P2 nicht bei L2 verfügbar

        await Assert.ThrowsAsync<OrderValidationException>(() => service.CreateOrderAsync(request));
        Assert.Empty(db.Orders);
        Assert.Empty(db.OrderItems);
    }

    [Fact]
    public async Task CreateOrderAsync_Valid_PersistsOrderWithFixedPriceAndClearsIndependentOfLaterOfferChanges()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        var request = ValidRequest(new CreateOrderItemRequest("P1", "L1", 2));

        var order = await service.CreateOrderAsync(request);

        Assert.Equal(29.99m, order.Items.Single().UnitPrice);
        Assert.Equal(59.98m, order.Total);

        // Preisänderung beim Lieferanten nach Bestellung darf die Bestellposition nicht mehr beeinflussen.
        var offer = await db.Offers.SingleAsync(o => o.ProductId == "P1" && o.SupplierId == "L1");
        offer.Price = 99.99m;
        await db.SaveChangesAsync();

        var reloaded = await service.GetOrderAsync(order.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(29.99m, reloaded.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task CreateOrderAsync_DifferentSuppliersForSameProduct_AreSeparateOrderItems()
    {
        using var db = TestDb.CreateWithFixtures();
        var service = new OrderService(db);

        var request = ValidRequest(
            new CreateOrderItemRequest("P1", "L1", 1),
            new CreateOrderItemRequest("P1", "L2", 3));

        var order = await service.CreateOrderAsync(request);

        Assert.Equal(2, order.Items.Count);
        Assert.Contains(order.Items, i => i is { SupplierId: "L1", UnitPrice: 29.99m, Quantity: 1 });
        Assert.Contains(order.Items, i => i is { SupplierId: "L2", UnitPrice: 27.50m, Quantity: 3 });
    }
}
