using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await categoryService.GetCategoriesAsync(cancellationToken));
    }
}
