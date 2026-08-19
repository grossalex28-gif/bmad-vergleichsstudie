using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/subcategories")]
public class SubcategoriesController(SubcategoryPropertyQueryService subcategoryPropertyQueryService) : ControllerBase
{
    [HttpGet("{subcategoryId}/properties")]
    public async Task<ActionResult<List<PropertyFilterOptionDto>>> GetProperties(
        string subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await subcategoryPropertyQueryService.GetPropertyFilterOptionsAsync(subcategoryId, cancellationToken);
        return Ok(result);
    }
}
