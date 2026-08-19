using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Services;

public class ProductService(ShopDbContext db)
{
    public const int PageSize = 12;

    public async Task<ProductListResponseDto> GetListAsync(ProductQuery query, CancellationToken ct = default)
    {
        var products = db.Products
            .Include(p => p.SubCategory).ThenInclude(s => s!.Category)
            .Include(p => p.Offers)
            .Include(p => p.Ratings)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SubCategoryId))
        {
            products = products.Where(p => p.SubCategoryId == query.SubCategoryId);
        }
        else if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            products = products.Where(p => p.SubCategory!.CategoryId == query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            products = products.Where(p => p.Name.Contains(term) || p.Description.Contains(term));
        }

        foreach (var (name, value) in query.Properties)
        {
            products = products.Where(p => p.Properties.Any(pp => pp.Name == name && pp.Value == value));
        }

        products = query.Sort switch
        {
            ProductSort.NameAsc => products.OrderBy(p => p.Name),
            ProductSort.NameDesc => products.OrderByDescending(p => p.Name),
            ProductSort.PriceAsc => products.OrderBy(p => p.Offers.Min(o => o.Price)),
            ProductSort.PriceDesc => products.OrderByDescending(p => p.Offers.Min(o => o.Price)),
            ProductSort.PopularityAsc => products.OrderBy(p => p.ViewCount),
            ProductSort.PopularityDesc => products.OrderByDescending(p => p.ViewCount),
            _ => products.OrderBy(p => p.Name),
        };

        var totalCount = await products.CountAsync(ct);
        var page = query.Page < 1 ? 1 : query.Page;

        var pageItems = await products
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        var items = pageItems.Select(ToListItem).ToList();

        return new ProductListResponseDto(
            items,
            page,
            PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)PageSize));
    }

    public async Task<ProductDetailDto?> GetDetailAndRegisterViewAsync(string id, CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.SubCategory).ThenInclude(s => s!.Category)
            .Include(p => p.Properties)
            .Include(p => p.Offers).ThenInclude(o => o.Supplier)
            .Include(p => p.Ratings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return null;
        }

        product.ViewCount++;
        await db.SaveChangesAsync(ct);

        return ToDetail(product);
    }

    public async Task<RatingDto?> RateProductAsync(string productId, CreateRatingRequest request, CancellationToken ct = default)
    {
        var productExists = await db.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
        {
            return null;
        }

        var existing = await db.Ratings
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.AuthorName == request.AuthorName, ct);

        var now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            existing.Stars = request.Stars;
            existing.CreatedAt = now;
        }
        else
        {
            db.Ratings.Add(new Rating
            {
                ProductId = productId,
                AuthorName = request.AuthorName,
                Stars = request.Stars,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(ct);

        var ratings = await db.Ratings.Where(r => r.ProductId == productId).Select(r => r.Stars).ToListAsync(ct);

        return new RatingDto(
            request.AuthorName,
            request.Stars,
            now,
            ratings.Count > 0 ? ratings.Average() : 0d,
            ratings.Count);
    }

    private static ProductListItemDto ToListItem(Product p) => new(
        p.Id,
        p.Name,
        p.SubCategoryId,
        p.SubCategory!.Name,
        p.SubCategory.CategoryId,
        p.SubCategory.Category!.Name,
        p.Offers.Count > 0 ? p.Offers.Min(o => o.Price) : 0m,
        p.ViewCount,
        p.Ratings.Count > 0 ? p.Ratings.Average(r => r.Stars) : 0d,
        p.Ratings.Count);

    private static ProductDetailDto ToDetail(Product p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.SubCategoryId,
        p.SubCategory!.Name,
        p.SubCategory.CategoryId,
        p.SubCategory.Category!.Name,
        p.Properties.ToDictionary(pp => pp.Name, pp => pp.Value),
        p.Ratings.Count > 0 ? p.Ratings.Average(r => r.Stars) : 0d,
        p.Ratings.Count,
        p.ViewCount,
        [.. p.Offers.Select(o => new OfferDto(o.SupplierId, o.Supplier!.Name, o.Price))]);
}
