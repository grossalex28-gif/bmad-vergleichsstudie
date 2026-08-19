using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await db.Categories
            .Include(c => c.Subcategories)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Subcategories
                    .OrderBy(s => s.Name)
                    .Select(s => new SubcategoryDto(s.Id, s.Name, s.Eigenschaften))
                    .ToList()))
            .ToList();
    }
}
