using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductListResponseDto>> GetList([FromQuery] ProductQuery query, CancellationToken ct)
    {
        var result = await productService.GetListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetDetail(string id, CancellationToken ct)
    {
        var product = await productService.GetDetailAndRegisterViewAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("{id}/ratings")]
    public async Task<ActionResult<RatingDto>> Rate(string id, [FromBody] CreateRatingRequest request, CancellationToken ct)
    {
        var rating = await productService.RateProductAsync(id, request, ct);
        return rating is null ? NotFound() : Ok(rating);
    }
}
