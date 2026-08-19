using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Tests;

public class OrderServiceTests
{
    private static readonly LieferdatenDto Lieferdaten = new("Max Mustermann", "Musterstr. 1", "12345", "Berlin", "DE");
    private static readonly KontaktdatenDto Kontakt = new("max@example.com", "0123456789");

    [Fact]
    public async Task CreateOrderAsync_Fails_WhenCartIsEmpty()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.CreateOrderAsync(new OrderCreateDto(Lieferdaten, Kontakt, []));

        Assert.False(result.Success);
        Assert.Empty(db.Orders.ToList());
    }

    [Fact]
    public async Task CreateOrderAsync_Fails_WhenQuantityIsNotPositive()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.CreateOrderAsync(new OrderCreateDto(Lieferdaten, Kontakt,
            [new PositionCreateDto("P1", "L1", 0)]));

        Assert.False(result.Success);
        Assert.Empty(db.Orders.ToList());
    }

    [Fact]
    public async Task CreateOrderAsync_Fails_WhenOfferNoLongerExists()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.CreateOrderAsync(new OrderCreateDto(Lieferdaten, Kontakt,
            [new PositionCreateDto("P2", "L2", 1)]));

        Assert.False(result.Success);
        Assert.Empty(db.Orders.ToList());
    }

    [Fact]
    public async Task CreateOrderAsync_RejectsEntireOrder_WhenOnePositionIsInvalid()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.CreateOrderAsync(new OrderCreateDto(Lieferdaten, Kontakt,
        [
            new PositionCreateDto("P1", "L1", 1),
            new PositionCreateDto("P2", "L2", 1)
        ]));

        Assert.False(result.Success);
        Assert.Empty(db.Orders.ToList());
        Assert.Empty(db.OrderItems.ToList());
    }

    [Fact]
    public async Task CreateOrderAsync_Succeeds_AndFreezesOfferPriceAtOrderTime()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.CreateOrderAsync(new OrderCreateDto(Lieferdaten, Kontakt,
            [new PositionCreateDto("P1", "L1", 2)]));

        Assert.True(result.Success);
        var position = Assert.Single(result.Order!.Positionen);
        Assert.Equal(29.99m, position.Preis);
        Assert.Equal(59.98m, result.Order.Gesamtsumme);

        var offer = db.Offers.First(o => o.ProductId == "P1" && o.SupplierId == "L1");
        offer.Preis = 999m;
        await db.SaveChangesAsync();

        var reloaded = await sut.GetOrderAsync(result.Order.Id);
        Assert.Equal(29.99m, reloaded!.Positionen[0].Preis);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsNull_ForUnknownId()
    {
        using var db = TestDbContextFactory.CreateWithSeedData();
        var sut = new OrderService(db);

        var result = await sut.GetOrderAsync(999);

        Assert.Null(result);
    }
}
