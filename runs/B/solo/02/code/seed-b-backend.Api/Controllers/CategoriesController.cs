using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.ParentCategoryId == null)
            .Include(c => c.Subcategories)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var result = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Subcategories
                .OrderBy(s => s.Name)
                .Select(s => new SubcategoryDto(s.Id, s.Name, s.PropertyNames))
                .ToList()
        )).ToList();

        return Ok(result);
    }
}
