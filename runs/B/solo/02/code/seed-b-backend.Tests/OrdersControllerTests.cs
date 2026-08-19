using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Controllers;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Tests;

public class OrdersControllerTests
{
    private static readonly OrderContactDto ValidContact = new("Max Mustermann", "Hauptstr. 1", "12345", "Berlin", "max@example.de");

    [Fact]
    public async Task CreateOrder_RejectsEmptyCart()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);

        var result = await controller.CreateOrder(new OrderCreateDto(ValidContact, []));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrder_RejectsInvalidQuantity()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);
        var items = new List<OrderItemCreateDto> { new(1, 1, 0) };

        var result = await controller.CreateOrder(new OrderCreateDto(ValidContact, items));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrder_RejectsUnavailableOffer()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);
        var items = new List<OrderItemCreateDto> { new(1, 999, 1) };

        var result = await controller.CreateOrder(new OrderCreateDto(ValidContact, items));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrder_RejectsWholeOrder_WhenOneOfSeveralPositionsIsInvalid()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);
        var items = new List<OrderItemCreateDto>
        {
            new(1, 1, 2),
            new(2, 999, 1),
        };

        var result = await controller.CreateOrder(new OrderCreateDto(ValidContact, items));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateOrder_FreezesCurrentOfferPrice_AndClearsNothingServerSide()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);
        var items = new List<OrderItemCreateDto> { new(1, 1, 2) };

        var result = await controller.CreateOrder(new OrderCreateDto(ValidContact, items));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<OrderDto>(created.Value);
        Assert.Single(dto.Items);
        Assert.Equal(29.99m, dto.Items[0].UnitPrice);
        Assert.Equal(2, dto.Items[0].Quantity);
        Assert.Equal(59.98m, dto.Total);
    }

    [Fact]
    public async Task CreateOrder_KeepsFrozenPrice_WhenOfferPriceChangesLater()
    {
        using var db = TestDbFactory.CreateSeededContext();
        var controller = new OrdersController(db);
        var createResult = await controller.CreateOrder(new OrderCreateDto(ValidContact, [new OrderItemCreateDto(1, 1, 1)]));
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var orderId = Assert.IsType<OrderDto>(created.Value).Id;

        var offer = db.ProductOffers.First(o => o.ProductId == 1 && o.SupplierId == 1);
        offer.Price = 999.99m;
        await db.SaveChangesAsync();

        var getResult = await controller.GetOrder(orderId);
        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var orderDto = Assert.IsType<OrderDto>(ok.Value);

        Assert.Equal(29.99m, orderDto.Items[0].UnitPrice);
    }
}
