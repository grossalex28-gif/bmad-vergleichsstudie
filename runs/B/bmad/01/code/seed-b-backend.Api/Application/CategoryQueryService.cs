using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class CategoryQueryService(AppDbContext db)
{
    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Subcategories = c.Subcategories
                    .OrderBy(s => s.Id)
                    .Select(s => new SubcategoryDto
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
