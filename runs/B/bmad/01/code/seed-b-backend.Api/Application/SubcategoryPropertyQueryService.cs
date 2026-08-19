using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class SubcategoryPropertyQueryService(AppDbContext db)
{
    public async Task<List<PropertyFilterOptionDto>> GetPropertyFilterOptionsAsync(
        string subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var definitions = await db.PropertyDefinitions
            .AsNoTracking()
            .Where(d => d.SubcategoryId == subcategoryId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(cancellationToken);

        var definitionIds = definitions.Select(d => d.Id).ToList();

        var values = await db.ProductProperties
            .AsNoTracking()
            .Where(pp => definitionIds.Contains(pp.PropertyDefinitionId))
            .Select(pp => new { pp.PropertyDefinitionId, pp.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        var valuesByDefinition = values.ToLookup(v => v.PropertyDefinitionId, v => v.Value);

        return definitions
            .Select(d => new PropertyFilterOptionDto
            {
                Name = d.Name,
                Values = valuesByDefinition[d.Id].OrderBy(v => v).ToList()
            })
            .ToList();
    }
}
