using System.Globalization;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class ProductQueryService(AppDbContext db)
{
    private const int PageSize = 20;

    public async Task<PagedResultDto<ProductListItemDto>> GetProductsAsync(int page, string? categoryId = null, string? subcategoryId = null, string? sortBy = null, string? sortDirection = null, string? search = null, string[]? attr = null)
    {
        page = page < 1 ? 1 : page;

        IQueryable<Product> query = db.Products.Include(p => p.Subcategory);

        if (!string.IsNullOrEmpty(subcategoryId))
        {
            query = query.Where(p => p.SubcategoryId == subcategoryId);
        }
        else if (!string.IsNullOrEmpty(categoryId))
        {
            query = query.Where(p => p.Subcategory!.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(loweredSearch) || p.Description.ToLower().Contains(loweredSearch));
        }

        var attributeFilters = ParseAttributeFilters(attr);
        if (attributeFilters.Count > 0)
        {
            var candidates = await query.Select(p => new { p.Id, p.Attributes }).ToListAsync();
            var matchingIds = candidates
                .Where(c => attributeFilters.All(f => ProductAttributeJson.Matches(c.Attributes, f.Name, f.Value)))
                .Select(c => c.Id)
                .ToList();
            query = query.Where(p => matchingIds.Contains(p.Id));
        }

        var availableAttributeFilters = string.IsNullOrEmpty(subcategoryId)
            ? (IReadOnlyList<AttributeFilterOptionDto>)Array.Empty<AttributeFilterOptionDto>()
            : await BuildAvailableAttributeFiltersAsync(subcategoryId);

        var totalCount = await query.CountAsync();

        // long arithmetic + clamp to totalCount avoids an int overflow into a negative
        // Skip() for very large page values while preserving "beyond last page -> empty" behavior.
        var skip = (int)Math.Min((long)(page - 1) * PageSize, totalCount);

        var descending = sortDirection == "desc";
        IOrderedQueryable<Product> orderedQuery = sortBy switch
        {
            "price" => descending
                ? query.OrderByDescending(p => p.Offers.Min(o => o.Price))
                : query.OrderBy(p => p.Offers.Min(o => o.Price)),
            "views" => query.OrderByDescending(p => p.ViewCount),
            _ => descending
                ? query.OrderByDescending(p => p.Name.ToLower())
                : query.OrderBy(p => p.Name.ToLower())
        };

        var items = await orderedQuery
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(PageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Subcategory!.Name,
                p.Offers.Min(o => o.Price)))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        return new PagedResultDto<ProductListItemDto>(items, page, PageSize, totalCount, totalPages, availableAttributeFilters);
    }

    private static List<(string Name, string Value)> ParseAttributeFilters(string[]? attr)
    {
        if (attr is null) { return []; }

        var result = new List<(string Name, string Value)>();
        foreach (var entry in attr)
        {
            var separatorIndex = entry.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex == entry.Length - 1) { continue; }
            result.Add((entry[..separatorIndex], entry[(separatorIndex + 1)..]));
        }
        return result;
    }

    private async Task<IReadOnlyList<AttributeFilterOptionDto>> BuildAvailableAttributeFiltersAsync(string subcategoryId)
    {
        var attributeNames = await db.Subcategories
            .Where(s => s.Id == subcategoryId)
            .Select(s => s.AttributeNames)
            .FirstOrDefaultAsync();

        if (attributeNames is null || attributeNames.Count == 0)
        {
            return Array.Empty<AttributeFilterOptionDto>();
        }

        var attributesJsonList = await db.Products
            .Where(p => p.SubcategoryId == subcategoryId)
            .Select(p => p.Attributes)
            .ToListAsync();

        var result = new List<AttributeFilterOptionDto>();
        foreach (var name in attributeNames)
        {
            var distinctValues = attributesJsonList
                .Select(json => ProductAttributeJson.TryFormatValue(json, name))
                .Where(v => v is not null)
                .Select(v => v!)
                .Distinct()
                .ToList();

            // Numeric attributes (e.g. LeistungWatt) must sort by value, not lexically ("1800" < "300"
            // under ordinal text comparison) — only falls back to ordinal text order for non-numeric attributes.
            var values = distinctValues.All(v => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                ? distinctValues.OrderBy(v => decimal.Parse(v, NumberStyles.Number, CultureInfo.InvariantCulture)).ToList()
                : distinctValues.OrderBy(v => v, StringComparer.Ordinal).ToList();
            result.Add(new AttributeFilterOptionDto(name, values));
        }
        return result;
    }
}
