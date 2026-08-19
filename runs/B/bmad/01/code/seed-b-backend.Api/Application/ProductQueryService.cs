using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class ProductQueryService(AppDbContext db)
{
    private const int PageSize = 20;
    private const int MaxSearchTermLength = 200;

    public async Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(
        int page = 1,
        string? categoryId = null,
        string? subcategoryId = null,
        string? sortBy = null,
        string? sortDir = null,
        string? q = null,
        IReadOnlyList<string>? property = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var query = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(subcategoryId))
        {
            query = query.Where(p => p.SubcategoryId == subcategoryId);
        }
        else if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(p => p.Subcategory.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var trimmed = q.Trim();
            var term = (trimmed.Length > MaxSearchTermLength ? trimmed[..MaxSearchTermLength] : trimmed).ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        if (property is { Count: > 0 })
        {
            var groups = property
                .Select(entry =>
                {
                    var separatorIndex = entry.IndexOf(':');
                    if (separatorIndex < 0)
                    {
                        return (Name: (string?)null, Value: (string?)null);
                    }

                    var name = entry[..separatorIndex].Trim();
                    var value = entry[(separatorIndex + 1)..].Trim();
                    return (Name: name, Value: value);
                })
                .Where(entry => !string.IsNullOrEmpty(entry.Name) && !string.IsNullOrEmpty(entry.Value))
                .GroupBy(entry => entry.Name!, entry => entry.Value!);

            foreach (var group in groups)
            {
                var name = group.Key;
                var values = group.ToList();
                query = query.Where(p => p.ProductProperties.Any(pp => pp.PropertyDefinition.Name == name && values.Contains(pp.Value)));
            }
        }

        var descending = string.Equals(sortDir?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.Trim().ToLowerInvariant() switch
        {
            "price" => descending
                ? query.OrderByDescending(p => p.Offers.Select(o => (decimal?)o.Price).Min()).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Offers.Select(o => (decimal?)o.Price).Min()).ThenBy(p => p.Id),
            "name" => descending
                ? query.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "viewcount" => descending
                ? query.OrderByDescending(p => p.ViewCount).ThenBy(p => p.Id)
                : query.OrderBy(p => p.ViewCount).ThenBy(p => p.Id),
            _ => query.OrderBy(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pageCount = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)PageSize);

        if (pageCount > 0 && page > pageCount)
        {
            page = pageCount;
        }

        var items = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                LowestPrice = p.Offers.Select(o => (decimal?)o.Price).Min()
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ProductListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageCount = pageCount,
            Page = page,
            PageSize = PageSize
        };
    }
}
