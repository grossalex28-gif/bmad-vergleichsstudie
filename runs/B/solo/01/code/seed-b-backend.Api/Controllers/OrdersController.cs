using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderCreateDto dto, CancellationToken cancellationToken)
    {
        if (dto.Lieferdaten is null || dto.Kontakt is null ||
            string.IsNullOrWhiteSpace(dto.Lieferdaten.Name) ||
            string.IsNullOrWhiteSpace(dto.Lieferdaten.Strasse) ||
            string.IsNullOrWhiteSpace(dto.Lieferdaten.Plz) ||
            string.IsNullOrWhiteSpace(dto.Lieferdaten.Ort) ||
            string.IsNullOrWhiteSpace(dto.Lieferdaten.Land) ||
            string.IsNullOrWhiteSpace(dto.Kontakt.Email) ||
            string.IsNullOrWhiteSpace(dto.Kontakt.Telefon))
        {
            return BadRequest(new { error = "Liefer- oder Kontaktdaten sind unvollständig." });
        }

        var result = await orderService.CreateOrderAsync(dto, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetOrder), new { id = result.Order!.Id }, result.Order);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
}
