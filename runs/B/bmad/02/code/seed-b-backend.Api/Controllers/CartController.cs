using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Controllers.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController(CartService cartService) : ControllerBase
{
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        [FromBody] AddCartItemRequest request,
        CancellationToken ct = default)
    {
        var parsedCartId = Guid.TryParse(cartId, out var parsed) ? parsed : (Guid?)null;

        var result = await cartService.AddItemAsync(parsedCartId, request.ProductId, request.SupplierId, request.Quantity, ct);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(result));
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        CancellationToken ct = default)
    {
        var parsedCartId = Guid.TryParse(cartId, out var parsed) ? parsed : (Guid?)null;
        var result = await cartService.GetCartAsync(parsedCartId, ct);
        return Ok(MapToDto(result));
    }

    [HttpPatch("items/{productId}/{supplierId}")]
    public async Task<ActionResult<CartDto>> UpdateItemQuantity(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        string productId,
        string supplierId,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken ct = default)
    {
        var parsedCartId = Guid.TryParse(cartId, out var parsed) ? parsed : (Guid?)null;
        if (parsedCartId is null)
        {
            return NotFound();
        }

        var result = await cartService.UpdateItemQuantityAsync(parsedCartId.Value, productId, supplierId, request.Quantity, ct);
        return result.Status switch
        {
            CartMutationStatus.ItemNotFound => NotFound(),
            CartMutationStatus.OfferGone => Conflict(new { error = "Angebot nicht mehr verfügbar" }),
            _ => Ok(MapToDto(result.Cart!))
        };
    }

    [HttpDelete("items/{productId}/{supplierId}")]
    public async Task<ActionResult<CartDto>> RemoveItem(
        [FromHeader(Name = "X-Cart-Id")] string? cartId,
        string productId,
        string supplierId,
        CancellationToken ct = default)
    {
        var parsedCartId = Guid.TryParse(cartId, out var parsed) ? parsed : (Guid?)null;
        var result = await cartService.RemoveItemAsync(parsedCartId, productId, supplierId, ct);
        return Ok(MapToDto(result));
    }

    private static CartDto MapToDto(CartResult result) => new()
    {
        CartId = result.CartId,
        TotalPrice = result.TotalPrice,
        Items = result.Items
            .Select(l => new CartItemDto
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                SupplierId = l.SupplierId,
                SupplierName = l.SupplierName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.UnitPrice * l.Quantity
            })
            .ToList()
    };
}
