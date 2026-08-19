using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Controllers.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(CatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken ct = default)
    {
        var categories = await catalogService.GetCategoriesAsync(ct);

        var response = categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Subcategories = c.Subcategories
                    .Select(s => new SubcategoryDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Properties = s.Properties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList()
                    })
                    .ToList()
            })
            .ToList();

        return Ok(response);
    }
}
