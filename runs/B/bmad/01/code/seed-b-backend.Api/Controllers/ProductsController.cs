using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ProductQueryService productQueryService, ProductDetailService productDetailService, RatingService ratingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductListItemDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] string? categoryId = null,
        [FromQuery] string? subcategoryId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] string? q = null,
        [FromQuery] string[]? property = null,
        CancellationToken cancellationToken = default)
    {
        var result = await productQueryService.GetProductsAsync(page, categoryId, subcategoryId, sortBy, sortDir, q, property, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string id, CancellationToken cancellationToken = default)
    {
        var result = await productDetailService.GetProductDetailAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("{id}/ratings")]
    public async Task<IActionResult> SubmitRating(string id, RatingCreateDto rating, CancellationToken cancellationToken = default)
    {
        var success = await ratingService.SubmitRatingAsync(id, rating.AuthorName, rating.Value, cancellationToken);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
