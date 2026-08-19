using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var order = await orderService.CreateOrderAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (OrderValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
    {
        var order = await orderService.GetOrderAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }
}
