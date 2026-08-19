using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Controllers.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(CatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetProducts([FromQuery] int page = 1, [FromQuery] string? categoryId = null, [FromQuery] string? subcategoryId = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null, [FromQuery] string? search = null, [FromQuery] Dictionary<string, string>? properties = null, CancellationToken ct = default)
    {
        var result = await catalogService.GetProductsAsync(page, categoryId, subcategoryId, sortBy, sortDirection, search, properties, ct);

        var response = new ProductListResponse
        {
            Items = result.Items.Select(p => new ProductSummaryDto { Id = p.Id, Name = p.Name }).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string id, CancellationToken ct = default)
    {
        var detail = await catalogService.GetProductDetailAsync(id, ct);
        if (detail is null)
        {
            return NotFound();
        }

        var product = detail.Product;
        var response = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Category = new CategoryRefDto { Id = product.Subcategory!.Category!.Id, Name = product.Subcategory.Category.Name },
            Subcategory = new SubcategoryRefDto { Id = product.Subcategory.Id, Name = product.Subcategory.Name },
            Properties = product.PropertyValues
                .OrderBy(pv => pv.Name, StringComparer.Ordinal)
                .Select(pv => new ProductPropertyDto { Name = pv.Name, Value = pv.Value })
                .ToList(),
            AverageRating = detail.AverageRating,
            RatingCount = detail.RatingCount,
            Offers = product.Offers
                .OrderBy(o => o.Price)
                .ThenBy(o => o.SupplierId, StringComparer.Ordinal)
                .Select(o => new SupplierOfferDto { SupplierId = o.SupplierId, SupplierName = o.Supplier!.Name, Price = o.Price })
                .ToList()
        };

        return Ok(response);
    }
}
