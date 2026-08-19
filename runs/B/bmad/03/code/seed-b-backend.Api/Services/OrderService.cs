using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class OrderService(AppDbContext db)
{
    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequestDto request)
    {
        if (request.Items is not { Count: > 0 })
        {
            return CreateOrderResult.Rejected("empty-cart", "Der Warenkorb ist leer.");
        }

        if (request.Items.Any(i => i is null || i.Quantity < 1))
        {
            return CreateOrderResult.Rejected("invalid-quantity", "Der Warenkorb enthält eine ungültige Menge.");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var offers = await db.Offers
            .Include(o => o.Product)
            .Include(o => o.Supplier)
            .Where(o => productIds.Contains(o.ProductId))
            .ToListAsync();
        var offersByKey = offers.ToDictionary(o => (o.ProductId, o.SupplierId));

        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            if (!offersByKey.TryGetValue((item.ProductId, item.SupplierId), out var offer))
            {
                return CreateOrderResult.Rejected("offer-unavailable", "Ein Angebot im Warenkorb ist nicht mehr verfügbar.");
            }

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = offer.ProductId,
                SupplierId = offer.SupplierId,
                ProductName = offer.Product!.Name,
                SupplierName = offer.Supplier!.Name,
                UnitPrice = offer.Price,
                Quantity = item.Quantity
            });
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Street = request.Street,
            PostalCode = request.PostalCode,
            City = request.City,
            Country = request.Country,
            Email = request.Email,
            Status = "Eingegangen",
            Items = orderItems
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return CreateOrderResult.Success(order.Id);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(Guid id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return null;
        }

        var items = order.Items
            .Select(i => new OrderItemDto(i.ProductName, i.SupplierName, i.UnitPrice, i.Quantity))
            .ToList();
        var totalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        return new OrderDetailDto(order.Id, order.Status, items, totalAmount);
    }
}
