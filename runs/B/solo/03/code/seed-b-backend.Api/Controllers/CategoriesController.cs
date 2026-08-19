using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await db.Categories
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var result = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            [.. c.SubCategories
                .OrderBy(s => s.Name)
                .Select(s => new SubCategoryDto(s.Id, s.Name, s.PropertyNames))]))
            .ToList();

        return Ok(result);
    }
}
