using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? categoryId,
        [FromQuery] string? subcategoryId,
        [FromQuery] string? sort,
        [FromQuery] int page,
        CancellationToken cancellationToken)
    {
        var eigenschaften = Request.Query
            .Where(q => q.Key.StartsWith("eig.", StringComparison.OrdinalIgnoreCase) && q.Key.Length > 4)
            .ToDictionary(q => q.Key[4..], q => q.Value.ToString());

        var query = new ProductQuery(search, categoryId, subcategoryId, ParseSort(sort), page < 1 ? 1 : page, eigenschaften);
        var result = await productService.GetProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductDetailAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("{id}/ratings")]
    public async Task<ActionResult<RatingResponseDto>> AddRating(string id, [FromBody] RatingCreateDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.AutorName))
        {
            return BadRequest(new { error = "Der Autorenname darf nicht leer sein." });
        }

        if (dto.Wert is < 1 or > 5)
        {
            return BadRequest(new { error = "Die Bewertung muss zwischen 1 und 5 liegen." });
        }

        var result = await productService.AddOrReplaceRatingAsync(id, dto.AutorName, dto.Wert, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private static ProductSort ParseSort(string? sort) => sort?.ToLowerInvariant() switch
    {
        "name_desc" => ProductSort.NameDesc,
        "price_asc" => ProductSort.PriceAsc,
        "price_desc" => ProductSort.PriceDesc,
        "views_asc" => ProductSort.ViewsAsc,
        "views_desc" => ProductSort.ViewsDesc,
        _ => ProductSort.NameAsc
    };
}
