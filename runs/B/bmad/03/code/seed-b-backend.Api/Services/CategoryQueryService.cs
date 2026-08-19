using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class CategoryQueryService(AppDbContext db)
{
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await db.Categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Subcategories
                    .OrderBy(s => s.Name)
                    .ThenBy(s => s.Id)
                    .Select(s => new SubcategoryDto(s.Id, s.Name))
                    .ToList()))
            .ToListAsync();
    }
}
