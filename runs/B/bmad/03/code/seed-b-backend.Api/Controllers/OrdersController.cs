using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        var result = await orderService.CreateOrderAsync(request);
        if (result.RejectionReason is not null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bestellung abgelehnt",
                detail: result.RejectionDetail,
                extensions: new Dictionary<string, object?> { ["reason"] = result.RejectionReason });
        }

        return Created($"/api/orders/{result.OrderId}", new OrderCreatedDto(result.OrderId!.Value));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await orderService.GetOrderAsync(id);
        return result is null ? NotFound() : Ok(result);
    }
}
