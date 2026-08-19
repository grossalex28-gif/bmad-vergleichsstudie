using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        // B-F14: the whole order is rejected on any violation, never a partial order.
        var errors = new List<OrderValidationErrorDto>();

        if (dto.Items is not { Count: > 0 })
        {
            errors.Add(new OrderValidationErrorDto("EMPTY_CART", "Der Warenkorb ist leer.", null));
            return BadRequest(new { errors });
        }

        for (var i = 0; i < dto.Items.Count; i++)
        {
            if (dto.Items[i].Quantity < 1)
            {
                errors.Add(new OrderValidationErrorDto("INVALID_QUANTITY", $"Ungültige Menge in Position {i + 1}.", i));
            }
        }

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var offers = await db.ProductOffers
            .Include(o => o.Product)
            .Include(o => o.Supplier)
            .Where(o => productIds.Contains(o.ProductId))
            .ToListAsync();
        var offersByKey = offers.ToDictionary(o => (o.ProductId, o.SupplierId));

        for (var i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            if (!offersByKey.ContainsKey((item.ProductId, item.SupplierId)))
            {
                errors.Add(new OrderValidationErrorDto(
                    "OFFER_UNAVAILABLE",
                    $"Das Produkt in Position {i + 1} wird vom gewählten Lieferanten nicht mehr angeboten.",
                    i));
            }
        }

        if (dto.Contact is null ||
            string.IsNullOrWhiteSpace(dto.Contact.RecipientName) ||
            string.IsNullOrWhiteSpace(dto.Contact.Street) ||
            string.IsNullOrWhiteSpace(dto.Contact.PostalCode) ||
            string.IsNullOrWhiteSpace(dto.Contact.City) ||
            string.IsNullOrWhiteSpace(dto.Contact.Email))
        {
            errors.Add(new OrderValidationErrorDto("INVALID_CONTACT", "Liefer- und Kontaktdaten sind unvollständig.", null));
        }

        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        var order = new Order
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OrderStatus.Received,
            RecipientName = dto.Contact!.RecipientName.Trim(),
            Street = dto.Contact.Street.Trim(),
            PostalCode = dto.Contact.PostalCode.Trim(),
            City = dto.Contact.City.Trim(),
            Email = dto.Contact.Email.Trim(),
        };

        foreach (var item in dto.Items)
        {
            var offer = offersByKey[(item.ProductId, item.SupplierId)];
            order.Items.Add(new OrderItem
            {
                ProductId = offer.ProductId,
                ProductName = offer.Product.Name,
                SupplierId = offer.SupplierId,
                SupplierName = offer.Supplier.Name,
                UnitPrice = offer.Price,
                Quantity = item.Quantity,
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = MapOrder(order);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(MapOrder(order));
    }

    private static OrderDto MapOrder(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemResultDto(i.ProductId, i.ProductName, i.SupplierId, i.SupplierName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList();

        return new OrderDto(
            order.Id,
            order.Status,
            order.CreatedAt,
            new OrderContactDto(order.RecipientName, order.Street, order.PostalCode, order.City, order.Email),
            items,
            items.Sum(i => i.LineTotal));
    }
}
