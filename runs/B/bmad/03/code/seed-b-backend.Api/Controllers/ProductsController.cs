using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ProductQueryService queryService, ProductService detailService, RatingService ratingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] string? categoryId = null, [FromQuery] string? subcategoryId = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null, [FromQuery] string? search = null, [FromQuery] string[]? attr = null)
    {
        var result = await queryService.GetProductsAsync(page, categoryId, subcategoryId, sortBy, sortDirection, search, attr);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var result = await detailService.GetProductDetailAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id}/ratings")]
    public async Task<IActionResult> PostRating(string id, [FromBody] RatingRequestDto request)
    {
        var result = await ratingService.UpsertRatingAsync(id, request.AuthorName, request.Value);
        return result is null ? NotFound() : Ok(result);
    }
}
