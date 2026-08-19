using System.Data;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class OrderService(AppDbContext context)
{
    public async Task<OrderPlacementResult> PlaceOrderAsync(
        Guid? cartId,
        string customerName,
        string deliveryAddress,
        string email,
        CancellationToken ct = default)
    {
        if (cartId is null)
        {
            return new OrderPlacementResult { Status = OrderPlacementStatus.CartEmpty };
        }

        var lines = await context.CartItems
            .Where(ci => ci.CartId == cartId)
            .Select(ci => new
            {
                ci.ProductId,
                ci.SupplierId,
                ci.Quantity,
                ProductName = ci.Product!.Name,
                SupplierName = ci.Supplier!.Name
            })
            .ToListAsync(ct);

        if (lines.Count == 0)
        {
            return new OrderPlacementResult { Status = OrderPlacementStatus.CartEmpty };
        }

        var invalidQuantityLines = lines
            .Where(l => l.Quantity < 1)
            .Select(l => new OrderLineRejection
            {
                ProductId = l.ProductId,
                SupplierId = l.SupplierId,
                Reason = OrderLineRejectionReason.InvalidQuantity
            })
            .ToList();
        if (invalidQuantityLines.Count > 0)
        {
            return new OrderPlacementResult { Status = OrderPlacementStatus.LinesRejected, RejectedLines = invalidQuantityLines };
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var offerPrices = new Dictionary<(string ProductId, string SupplierId), decimal>();
        var offerGoneLines = new List<OrderLineRejection>();
        foreach (var line in lines)
        {
            var price = await context.Offers
                .Where(o => o.ProductId == line.ProductId && o.SupplierId == line.SupplierId)
                .Select(o => (decimal?)o.Price)
                .SingleOrDefaultAsync(ct);

            if (price is null)
            {
                offerGoneLines.Add(new OrderLineRejection
                {
                    ProductId = line.ProductId,
                    SupplierId = line.SupplierId,
                    Reason = OrderLineRejectionReason.OfferGone
                });
            }
            else
            {
                offerPrices[(line.ProductId, line.SupplierId)] = price.Value;
            }
        }

        if (offerGoneLines.Count > 0)
        {
            await transaction.RollbackAsync(ct);
            return new OrderPlacementResult { Status = OrderPlacementStatus.LinesRejected, RejectedLines = offerGoneLines };
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            DeliveryAddress = deliveryAddress,
            Email = email
        };
        context.Orders.Add(order);

        var resultLines = new List<OrderResultLine>();
        foreach (var line in lines)
        {
            var unitPrice = offerPrices[(line.ProductId, line.SupplierId)];
            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = line.ProductId,
                SupplierId = line.SupplierId,
                Quantity = line.Quantity,
                UnitPrice = unitPrice
            });
            resultLines.Add(new OrderResultLine
            {
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                SupplierId = line.SupplierId,
                SupplierName = line.SupplierName,
                Quantity = line.Quantity,
                UnitPrice = unitPrice
            });

            // Deletes only the exact snapshot line validated above (not the live cart),
            // so any item added to the cart during this checkout is left untouched
            // instead of being silently discarded by a blanket cart-wide delete.
            await context.CartItems
                .Where(ci => ci.CartId == cartId && ci.ProductId == line.ProductId && ci.SupplierId == line.SupplierId)
                .ExecuteDeleteAsync(ct);
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new OrderPlacementResult
        {
            Status = OrderPlacementStatus.Success,
            Order = new OrderResult
            {
                OrderId = order.Id,
                Status = order.Status,
                Items = resultLines,
                TotalPrice = resultLines.Sum(l => l.UnitPrice * l.Quantity)
            }
        };
    }

    public async Task<OrderResult?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await context.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return null;
        }

        var items = await context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Select(oi => new OrderResultLine
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product!.Name,
                SupplierId = oi.SupplierId,
                SupplierName = oi.Supplier!.Name,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            })
            .ToListAsync(ct);

        return new OrderResult
        {
            OrderId = order.Id,
            Status = order.Status,
            Items = items,
            TotalPrice = items.Sum(l => l.UnitPrice * l.Quantity)
        };
    }
}
