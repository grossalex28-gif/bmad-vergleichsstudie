using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class CartService(AppDbContext context)
{
    public async Task<CartResult?> AddItemAsync(
        Guid? cartId,
        string productId,
        string supplierId,
        int quantity,
        CancellationToken ct = default)
    {
        var offerExists = await context.Offers
            .AnyAsync(o => o.ProductId == productId && o.SupplierId == supplierId, ct);
        if (!offerExists)
        {
            return null;
        }

        var resolvedCartId = cartId is not null && await context.Carts.AnyAsync(c => c.Id == cartId, ct)
            ? cartId.Value
            : await CreateCartAsync(ct);

        var updatedRows = await context.CartItems
            .Where(ci => ci.CartId == resolvedCartId && ci.ProductId == productId && ci.SupplierId == supplierId)
            .ExecuteUpdateAsync(s => s.SetProperty(ci => ci.Quantity, ci => ci.Quantity + quantity), ct);

        if (updatedRows == 0)
        {
            var newItem = new CartItem { CartId = resolvedCartId, ProductId = productId, SupplierId = supplierId, Quantity = quantity };
            context.CartItems.Add(newItem);
            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Ein gleichzeitiges Hinzufügen desselben Produkt-Lieferant-Paars zu
                // diesem Warenkorb hat seine Zeile zwischen unserem ExecuteUpdateAsync
                // (traf nichts) und diesem Add eingefügt — der DB-Unique-Constraint
                // (zusammengesetzter Primärschlüssel, AD-1) lehnt unseren Insert ab,
                // statt still zu duplizieren. Fallback: Update, das jetzt garantiert
                // genau diese Zeile trifft. Identisches Muster wie RatingService
                // (Story 2.3) für den AD-5-Race.
                context.Entry(newItem).State = EntityState.Detached;
                var raceFallbackRows = await context.CartItems
                    .Where(ci => ci.CartId == resolvedCartId && ci.ProductId == productId && ci.SupplierId == supplierId)
                    .ExecuteUpdateAsync(s => s.SetProperty(ci => ci.Quantity, ci => ci.Quantity + quantity), ct);

                if (raceFallbackRows == 0)
                {
                    throw;
                }
            }
        }

        return await BuildCartResultAsync(resolvedCartId, ct);
    }

    private async Task<Guid> CreateCartAsync(CancellationToken ct)
    {
        var cart = new Cart { Id = Guid.NewGuid() };
        context.Carts.Add(cart);
        await context.SaveChangesAsync(ct);
        return cart.Id;
    }

    private async Task<CartResult> BuildCartResultAsync(Guid cartId, CancellationToken ct)
    {
        var lines = await context.CartItems
            .Where(ci => ci.CartId == cartId)
            .Join(context.Offers,
                ci => new { ci.ProductId, ci.SupplierId },
                o => new { o.ProductId, o.SupplierId },
                (ci, o) => new CartResultLine
                {
                    ProductId = ci.ProductId,
                    ProductName = o.Product!.Name,
                    SupplierId = ci.SupplierId,
                    SupplierName = o.Supplier!.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = o.Price
                })
            .ToListAsync(ct);

        return new CartResult
        {
            CartId = cartId,
            Items = lines,
            TotalPrice = lines.Sum(l => l.UnitPrice * l.Quantity)
        };
    }

    public async Task<CartResult> GetCartAsync(Guid? cartId, CancellationToken ct = default)
    {
        if (cartId is null || !await context.Carts.AnyAsync(c => c.Id == cartId, ct))
        {
            return new CartResult { CartId = null, Items = [], TotalPrice = 0 };
        }

        return await BuildCartResultAsync(cartId.Value, ct);
    }

    public async Task<CartMutationResult> UpdateItemQuantityAsync(
        Guid cartId,
        string productId,
        string supplierId,
        int quantity,
        CancellationToken ct = default)
    {
        var itemExists = await context.CartItems
            .AnyAsync(ci => ci.CartId == cartId && ci.ProductId == productId && ci.SupplierId == supplierId, ct);
        if (!itemExists)
        {
            return new CartMutationResult { Status = CartMutationStatus.ItemNotFound };
        }

        var offerExists = await context.Offers
            .AnyAsync(o => o.ProductId == productId && o.SupplierId == supplierId, ct);
        if (!offerExists)
        {
            return new CartMutationResult { Status = CartMutationStatus.OfferGone };
        }

        await context.CartItems
            .Where(ci => ci.CartId == cartId && ci.ProductId == productId && ci.SupplierId == supplierId)
            .ExecuteUpdateAsync(s => s.SetProperty(ci => ci.Quantity, quantity), ct);

        return new CartMutationResult
        {
            Status = CartMutationStatus.Success,
            Cart = await BuildCartResultAsync(cartId, ct)
        };
    }

    public async Task<CartResult> RemoveItemAsync(
        Guid? cartId,
        string productId,
        string supplierId,
        CancellationToken ct = default)
    {
        if (cartId is null || !await context.Carts.AnyAsync(c => c.Id == cartId, ct))
        {
            return new CartResult { CartId = null, Items = [], TotalPrice = 0 };
        }

        await context.CartItems
            .Where(ci => ci.CartId == cartId && ci.ProductId == productId && ci.SupplierId == supplierId)
            .ExecuteDeleteAsync(ct);

        return await BuildCartResultAsync(cartId.Value, ct);
    }
}
