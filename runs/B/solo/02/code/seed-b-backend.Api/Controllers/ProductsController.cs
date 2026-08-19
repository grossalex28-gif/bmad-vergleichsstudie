using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // B-F1: the page size is fixed, not client-configurable.
    private const int PageSize = 12;

    private static readonly HashSet<string> ReservedQueryParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "categoryId", "subcategoryId", "search", "sortBy", "sortDir", "page",
    };

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductListItemDto>>> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] int? subcategoryId,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        IQueryable<Product> query = db.Products
            .AsNoTracking()
            .Include(p => p.Subcategory).ThenInclude(s => s.ParentCategory)
            .Include(p => p.Offers)
            .Include(p => p.Reviews);

        if (subcategoryId is not null)
        {
            query = query.Where(p => p.SubcategoryId == subcategoryId);
        }
        else if (categoryId is not null)
        {
            query = query.Where(p => p.Subcategory.ParentCategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        var products = await query.ToListAsync();

        // Category-specific properties are stored as JSON and filtered in memory;
        // the seeded catalog is small enough that this is not a concern.
        var propertyFilters = Request.Query
            .Where(kv => !ReservedQueryParams.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString()!, StringComparer.OrdinalIgnoreCase);

        if (propertyFilters.Count > 0)
        {
            products = products.Where(p => propertyFilters.All(f =>
                p.Properties.TryGetValue(f.Key, out var value) &&
                string.Equals(value, f.Value, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        IEnumerable<ProductListItemDto> items = products.Select(p => new ProductListItemDto(
            p.Id,
            p.Name,
            p.SubcategoryId,
            p.Subcategory.Name,
            p.Subcategory.ParentCategoryId ?? 0,
            p.Subcategory.ParentCategory?.Name ?? string.Empty,
            p.Offers.Count > 0 ? p.Offers.Min(o => o.Price) : null,
            p.Reviews.Count > 0 ? p.Reviews.Average(r => r.Rating) : null,
            p.Reviews.Count,
            p.ViewCount));

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        items = sortBy?.ToLowerInvariant() switch
        {
            "price" => descending
                ? items.OrderByDescending(i => i.MinPrice ?? decimal.MaxValue)
                : items.OrderBy(i => i.MinPrice ?? decimal.MaxValue),
            "popularity" => descending
                ? items.OrderByDescending(i => i.ViewCount)
                : items.OrderBy(i => i.ViewCount),
            "name" => descending
                ? items.OrderByDescending(i => i.Name)
                : items.OrderBy(i => i.Name),
            _ => items.OrderBy(i => i.Name),
        };

        var itemsList = items.ToList();
        var pageItems = itemsList.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        return Ok(new PagedResultDto<ProductListItemDto>(pageItems, itemsList.Count, page, PageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
    {
        var product = await db.Products
            .Include(p => p.Subcategory).ThenInclude(s => s.ParentCategory)
            .Include(p => p.Offers).ThenInclude(o => o.Supplier)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        // B-F13: every detail view increments the persistent view counter.
        product.ViewCount++;
        await db.SaveChangesAsync();

        var dto = new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.SubcategoryId,
            product.Subcategory.Name,
            product.Subcategory.ParentCategoryId ?? 0,
            product.Subcategory.ParentCategory?.Name ?? string.Empty,
            product.Properties,
            product.Reviews.Count > 0 ? product.Reviews.Average(r => r.Rating) : null,
            product.Reviews.Count,
            product.ViewCount,
            product.Offers.Select(o => new ProductOfferDto(o.SupplierId, o.Supplier.Name, o.Price)).ToList());

        return Ok(dto);
    }

    [HttpPost("{id:int}/reviews")]
    public async Task<ActionResult<ReviewResultDto>> AddReview(int id, [FromBody] ReviewCreateDto dto)
    {
        if (dto.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Die Bewertung muss zwischen 1 und 5 liegen." });
        }

        var authorName = dto.AuthorName?.Trim() ?? string.Empty;
        if (authorName.Length == 0)
        {
            return BadRequest(new { message = "Der Autorenname darf nicht leer sein." });
        }

        var productExists = await db.Products.AnyAsync(p => p.Id == id);
        if (!productExists)
        {
            return NotFound();
        }

        // B-F12: a repeated review by the same author replaces the previous one.
        var existing = await db.Reviews.FirstOrDefaultAsync(r => r.ProductId == id && r.AuthorName == authorName);
        if (existing is not null)
        {
            existing.Rating = dto.Rating;
            existing.CreatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.Reviews.Add(new Review
            {
                ProductId = id,
                AuthorName = authorName,
                Rating = dto.Rating,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        var reviews = await db.Reviews.Where(r => r.ProductId == id).ToListAsync();
        var average = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
        return Ok(new ReviewResultDto(average, reviews.Count));
    }
}
