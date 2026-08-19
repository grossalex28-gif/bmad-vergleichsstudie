using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Services;

public class OrderService(AppDbContext db) : IOrderService
{
    public async Task<OrderCreationResult> CreateOrderAsync(OrderCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Positionen is null || dto.Positionen.Count == 0)
        {
            return OrderCreationResult.Fail("Der Warenkorb ist leer.");
        }

        foreach (var pos in dto.Positionen)
        {
            if (pos.Menge < 1)
            {
                return OrderCreationResult.Fail($"Ungültige Menge für Produkt {pos.ProduktId}.");
            }
        }

        var resolvedItems = new List<(Offer Offer, PositionCreateDto Position)>();
        foreach (var pos in dto.Positionen)
        {
            var offer = await db.Offers
                .Include(o => o.Product)
                .Include(o => o.Supplier)
                .FirstOrDefaultAsync(o => o.ProductId == pos.ProduktId && o.SupplierId == pos.LieferantId, cancellationToken);

            if (offer is null)
            {
                return OrderCreationResult.Fail($"Produkt {pos.ProduktId} wird vom gewählten Lieferanten nicht mehr angeboten.");
            }

            resolvedItems.Add((offer, pos));
        }

        var order = new Order
        {
            Status = "Eingegangen",
            ErstelltAm = DateTime.UtcNow,
            LieferName = dto.Lieferdaten.Name,
            LieferStrasse = dto.Lieferdaten.Strasse,
            LieferPlz = dto.Lieferdaten.Plz,
            LieferOrt = dto.Lieferdaten.Ort,
            LieferLand = dto.Lieferdaten.Land,
            KontaktEmail = dto.Kontakt.Email,
            KontaktTelefon = dto.Kontakt.Telefon
        };

        foreach (var (offer, pos) in resolvedItems)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = offer.ProductId,
                ProductName = offer.Product!.Name,
                SupplierId = offer.SupplierId,
                SupplierName = offer.Supplier!.Name,
                Preis = offer.Preis,
                Menge = pos.Menge
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return OrderCreationResult.Ok(MapToDto(order));
    }

    public async Task<OrderResponseDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        return order is null ? null : MapToDto(order);
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        var positionen = order.Items
            .Select(i => new OrderItemResponseDto(i.ProductId, i.ProductName, i.SupplierId, i.SupplierName, i.Preis, i.Menge, i.Preis * i.Menge))
            .ToList();

        return new OrderResponseDto(
            order.Id,
            order.Status,
            order.ErstelltAm,
            new LieferdatenDto(order.LieferName, order.LieferStrasse, order.LieferPlz, order.LieferOrt, order.LieferLand),
            new KontaktdatenDto(order.KontaktEmail, order.KontaktTelefon),
            positionen,
            positionen.Sum(p => p.Zwischensumme));
    }
}
