using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class CatalogService(AppDbContext context)
{
    public const int PageSize = 20;

    public async Task<PagedResult<Product>> GetProductsAsync(
        int page,
        string? categoryId = null,
        string? subcategoryId = null,
        string? sortBy = null,
        string? sortDirection = null,
        string? search = null,
        IReadOnlyDictionary<string, string>? propertyFilters = null,
        CancellationToken ct = default)
    {
        var query = context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(subcategoryId))
        {
            query = query.Where(p => p.SubcategoryId == subcategoryId);
        }
        else if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(p => p.Subcategory!.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(normalizedSearch) ||
                p.Description.ToLower().Contains(normalizedSearch));
        }

        if (propertyFilters is not null)
        {
            foreach (var (propertyName, propertyValue) in propertyFilters)
            {
                var name = propertyName;
                var value = propertyValue;
                query = query.Where(p => p.PropertyValues.Any(pv => pv.Name == name && pv.Value == value));
            }
        }

        var totalCount = await query.CountAsync(ct);

        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "price" => descending
                ? query.OrderBy(p => p.Offers.Any() ? 0 : 1)
                       .ThenByDescending(p => p.Offers.Min(o => (decimal?)o.Price))
                       .ThenBy(p => p.Id)
                : query.OrderBy(p => p.Offers.Any() ? 0 : 1)
                       .ThenBy(p => p.Offers.Min(o => (decimal?)o.Price))
                       .ThenBy(p => p.Id),
            "name" => descending
                ? query.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "viewcount" => descending
                ? query.OrderByDescending(p => p.ViewCount).ThenBy(p => p.Id)
                : query.OrderBy(p => p.ViewCount).ThenBy(p => p.Id),
            _ => query.OrderBy(p => p.Id)
        };

        var skip = (long)(page - 1) * PageSize;

        var items = page < 1 || skip > int.MaxValue
            ? new List<Product>()
            : await query.Skip((int)skip).Take(PageSize).ToListAsync(ct);

        return new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDetail?> GetProductDetailAsync(string id, CancellationToken ct = default)
    {
        var updatedRows = await context.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

        if (updatedRows == 0)
        {
            return null;
        }

        // ExecuteUpdateAsync bypasses the change tracker, so a tracked Product from an
        // earlier query in this context would otherwise be returned below with its
        // stale, pre-increment ViewCount via EF Core's identity resolution. AsNoTracking
        // makes this query always materialize fresh data instead of resolving from the
        // identity map, scoped to this query only (unlike ChangeTracker.Clear(), it can't
        // discard unrelated pending changes elsewhere on this context).
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Subcategory!).ThenInclude(s => s!.Category)
            .Include(p => p.PropertyValues)
            .Include(p => p.Offers).ThenInclude(o => o.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return null;
        }

        var scores = await context.Ratings
            .Where(r => r.ProductId == id)
            .Select(r => r.Score)
            .ToListAsync(ct);

        return new ProductDetail
        {
            Product = product,
            AverageRating = scores.Count > 0 ? scores.Average() : null,
            RatingCount = scores.Count
        };
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await context.Categories
            .Include(c => c.Subcategories.OrderBy(s => s.Id))
            .ThenInclude(s => s.Properties)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
    }
}
