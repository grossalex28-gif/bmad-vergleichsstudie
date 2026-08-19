using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class OrderService(AppDbContext db)
{
    public async Task<OrderCreateResult> CreateOrderAsync(OrderCreateDto request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            return OrderCreateResult.Invalid("Der Warenkorb ist leer.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var order = new Order
        {
            PublicId = Guid.NewGuid(),
            Status = OrderStatus.Received,
            DeliveryName = request.Delivery.Name,
            DeliveryStreet = request.Delivery.Street,
            DeliveryPostalCode = request.Delivery.PostalCode,
            DeliveryCity = request.Delivery.City,
            DeliveryCountry = request.Delivery.Country,
            DeliveryEmail = request.Delivery.Email,
            DeliveryPhone = request.Delivery.Phone,
            CreatedAt = DateTime.UtcNow,
        };

        var responseItems = new List<OrderItemResponseDto>();
        var invalidLines = new List<InvalidLineDto>();

        foreach (var line in request.Items)
        {
            if (line.Quantity < 1)
            {
                invalidLines.Add(new InvalidLineDto
                {
                    ProductId = line.ProductId,
                    SupplierId = line.SupplierId,
                    Reason = OrderRejectionReasons.InvalidQuantity,
                });
                continue;
            }

            var offer = await db.Offers
                .Where(o => o.ProductId == line.ProductId && o.SupplierId == line.SupplierId)
                .Select(o => new { o.Price, ProductName = o.Product.Name, SupplierName = o.Supplier.Name })
                .SingleOrDefaultAsync(cancellationToken);

            if (offer is null)
            {
                invalidLines.Add(new InvalidLineDto
                {
                    ProductId = line.ProductId,
                    SupplierId = line.SupplierId,
                    Reason = OrderRejectionReasons.OfferNotFound,
                });
                continue;
            }

            order.Items.Add(new OrderItem
            {
                ProductId = line.ProductId,
                SupplierId = line.SupplierId,
                Quantity = line.Quantity,
                UnitPriceAtOrder = offer.Price,
            });

            responseItems.Add(new OrderItemResponseDto
            {
                ProductId = line.ProductId,
                ProductName = offer.ProductName,
                SupplierId = line.SupplierId,
                SupplierName = offer.SupplierName,
                UnitPrice = offer.Price,
                Quantity = line.Quantity,
                LineTotal = offer.Price * line.Quantity,
            });
        }

        if (invalidLines.Count > 0)
        {
            return OrderCreateResult.Invalid("Ihre Bestellung enthält ungültige Positionen.", invalidLines);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OrderCreateResult.Ok(new OrderDto
        {
            PublicId = order.PublicId,
            Status = order.Status.ToString(),
            Delivery = request.Delivery,
            Items = responseItems,
            TotalAmount = responseItems.Sum(i => i.LineTotal),
            CreatedAt = order.CreatedAt,
        });
    }

    public async Task<OrderDto?> GetOrderByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Items).ThenInclude(i => i.Supplier)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var items = order.Items
            .OrderBy(i => i.Id)
            .Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                SupplierId = i.SupplierId,
                SupplierName = i.Supplier.Name,
                UnitPrice = i.UnitPriceAtOrder,
                Quantity = i.Quantity,
                LineTotal = i.UnitPriceAtOrder * i.Quantity,
            })
            .ToList();

        return new OrderDto
        {
            PublicId = order.PublicId,
            Status = order.Status.ToString(),
            Delivery = new DeliveryDto
            {
                Name = order.DeliveryName,
                Street = order.DeliveryStreet,
                PostalCode = order.DeliveryPostalCode,
                City = order.DeliveryCity,
                Country = order.DeliveryCountry,
                Email = order.DeliveryEmail,
                Phone = order.DeliveryPhone,
            },
            Items = items,
            TotalAmount = items.Sum(i => i.LineTotal),
            CreatedAt = order.CreatedAt,
        };
    }
}
