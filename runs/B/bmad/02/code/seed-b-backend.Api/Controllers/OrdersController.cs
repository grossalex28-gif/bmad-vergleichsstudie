using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Controllers.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> PlaceOrder(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        [FromBody] PlaceOrderRequest request,
        CancellationToken ct = default)
    {
        var parsedCartId = Guid.TryParse(cartId, out var parsed) ? parsed : (Guid?)null;

        var result = await orderService.PlaceOrderAsync(
            parsedCartId, request.CustomerName, request.DeliveryAddress, request.Email, ct);

        return result.Status switch
        {
            OrderPlacementStatus.CartEmpty => Conflict(new OrderRejectionDto
            {
                Error = "Warenkorb ist leer",
                Lines = []
            }),
            OrderPlacementStatus.LinesRejected => Conflict(new OrderRejectionDto
            {
                Error = "Bestellung abgelehnt",
                Lines = result.RejectedLines!
                    .Select(l => new OrderRejectionLineDto
                    {
                        ProductId = l.ProductId,
                        SupplierId = l.SupplierId,
                        Reason = l.Reason == OrderLineRejectionReason.InvalidQuantity
                            ? "unzulaessige_menge"
                            : "angebot_nicht_mehr_verfuegbar"
                    })
                    .ToList()
            }),
            _ => StatusCode(StatusCodes.Status201Created, MapToDto(result.Order!))
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id, CancellationToken ct = default)
    {
        var result = await orderService.GetOrderAsync(id, ct);
        return result is null ? NotFound() : MapToDto(result);
    }

    private static OrderDto MapToDto(OrderResult result) => new()
    {
        OrderId = result.OrderId,
        Status = result.Status,
        TotalPrice = result.TotalPrice,
        Items = result.Items
            .Select(l => new OrderItemDto
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                SupplierId = l.SupplierId,
                SupplierName = l.SupplierName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.UnitPrice * l.Quantity
            })
            .ToList()
    };
}
