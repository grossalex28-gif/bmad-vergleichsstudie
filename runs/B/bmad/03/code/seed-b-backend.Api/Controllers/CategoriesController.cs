using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Services;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(CategoryQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await service.GetCategoriesAsync();
        return Ok(result);
    }
}
