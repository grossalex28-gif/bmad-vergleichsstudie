using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Services;

public class OrderService(ShopDbContext db)
{
    // B-F14: Eine Bestellung wird als Ganzes abgelehnt, wenn der Warenkorb leer ist,
    // eine Position eine unzulässige Menge enthält oder ein Produkt beim gewählten
    // Lieferanten nicht mehr angeboten wird. Es entsteht dabei keine Teilbestellung,
    // daher wird vollständig validiert, bevor irgendetwas gespeichert wird.
    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
        {
            throw new OrderValidationException("Der Warenkorb ist leer.");
        }

        foreach (var item in request.Items)
        {
            if (item.Quantity < 1)
            {
                throw new OrderValidationException(
                    $"Ungültige Menge für Produkt {item.ProductId}.");
            }
        }

        var order = new Order
        {
            ContactName = request.ContactName,
            Email = request.Email,
            Street = request.Street,
            PostalCode = request.PostalCode,
            City = request.City,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OrderStatus.Eingegangen,
        };

        foreach (var item in request.Items)
        {
            var offer = await db.Offers
                .Include(o => o.Product)
                .Include(o => o.Supplier)
                .FirstOrDefaultAsync(o => o.ProductId == item.ProductId && o.SupplierId == item.SupplierId, ct);

            if (offer is null || offer.Product is null || offer.Supplier is null)
            {
                throw new OrderValidationException(
                    $"Produkt {item.ProductId} wird beim gewählten Lieferanten nicht mehr angeboten.");
            }

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
        await db.SaveChangesAsync(ct);

        return ToDto(order);
    }

    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? null : ToDto(order);
    }

    public static OrderDto ToDto(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.CreatedAt,
        order.ContactName,
        order.Email,
        order.Street,
        order.PostalCode,
        order.City,
        [.. order.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, i.SupplierId, i.SupplierName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))],
        order.Items.Sum(i => i.UnitPrice * i.Quantity));
}
