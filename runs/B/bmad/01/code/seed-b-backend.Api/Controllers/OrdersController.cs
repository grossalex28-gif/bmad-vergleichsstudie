using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(OrderCreateDto request, CancellationToken cancellationToken = default)
    {
        var result = await orderService.CreateOrderAsync(request, cancellationToken);
        if (!result.Success)
        {
            var problemResult = (ObjectResult)Problem(detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
            if (result.InvalidLines is { Count: > 0 } && problemResult.Value is ProblemDetails problemDetails)
            {
                problemDetails.Extensions["invalidLines"] = result.InvalidLines;
            }
            return problemResult;
        }

        return StatusCode(StatusCodes.Status201Created, result.Order);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid publicId, CancellationToken cancellationToken = default)
    {
        var order = await orderService.GetOrderByPublicIdAsync(publicId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}
