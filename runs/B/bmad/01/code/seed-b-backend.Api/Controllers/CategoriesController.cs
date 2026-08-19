using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(CategoryQueryService categoryQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken cancellationToken = default)
    {
        var result = await categoryQueryService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }
}
