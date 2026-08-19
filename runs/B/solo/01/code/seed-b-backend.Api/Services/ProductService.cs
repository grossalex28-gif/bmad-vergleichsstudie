using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Services;

public class ProductService(AppDbContext db) : IProductService
{
    private const int PageSize = 12;

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var productsQuery = db.Products
            .Include(p => p.Subcategory).ThenInclude(s => s!.Category)
            .Include(p => p.Offers)
            .Include(p => p.Ratings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SubcategoryId))
        {
            productsQuery = productsQuery.Where(p => p.SubcategoryId == query.SubcategoryId);
        }
        else if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            productsQuery = productsQuery.Where(p => p.Subcategory!.CategoryId == query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(p => p.Name.ToLower().Contains(term) || p.Beschreibung.ToLower().Contains(term));
        }

        var candidates = await productsQuery.ToListAsync(cancellationToken);

        var enriched = candidates
            .Select(p => new ProductComputed(
                p,
                ParseEigenschaften(p.EigenschaftenJson),
                p.Offers.Count > 0 ? p.Offers.Min(o => o.Preis) : null,
                p.Ratings.Count > 0 ? p.Ratings.Average(r => r.Wert) : null,
                p.Ratings.Count))
            .ToList();

        if (query.Eigenschaften.Count > 0)
        {
            enriched = enriched.Where(e => query.Eigenschaften.All(kv =>
                    e.Eigenschaften.TryGetValue(kv.Key, out var value) &&
                    string.Equals(NormalizeJsonValue(value), kv.Value.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        IEnumerable<ProductComputed> sorted = query.Sort switch
        {
            ProductSort.NameAsc => enriched.OrderBy(e => e.Product.Name, StringComparer.OrdinalIgnoreCase),
            ProductSort.NameDesc => enriched.OrderByDescending(e => e.Product.Name, StringComparer.OrdinalIgnoreCase),
            ProductSort.PriceAsc => enriched.OrderBy(e => e.MinPreis is null).ThenBy(e => e.MinPreis),
            ProductSort.PriceDesc => enriched.OrderBy(e => e.MinPreis is null).ThenByDescending(e => e.MinPreis),
            ProductSort.ViewsAsc => enriched.OrderBy(e => e.Product.Aufrufe),
            ProductSort.ViewsDesc => enriched.OrderByDescending(e => e.Product.Aufrufe),
            _ => enriched.OrderBy(e => e.Product.Name, StringComparer.OrdinalIgnoreCase)
        };

        var sortedList = sorted.ToList();
        var totalCount = sortedList.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)PageSize);
        var page = Math.Max(1, query.Page);

        var items = sortedList
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(e => new ProductListItemDto(
                e.Product.Id,
                e.Product.Name,
                e.Product.Beschreibung,
                e.Product.Subcategory!.CategoryId,
                e.Product.Subcategory!.Category!.Name,
                e.Product.SubcategoryId,
                e.Product.Subcategory!.Name,
                e.Eigenschaften,
                e.MinPreis,
                e.DurchschnittsBewertung,
                e.AnzahlBewertungen,
                e.Product.Aufrufe))
            .ToList();

        return new PagedResult<ProductListItemDto>(items, page, PageSize, totalCount, totalPages);
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await db.Products
            .Include(p => p.Subcategory).ThenInclude(s => s!.Category)
            .Include(p => p.Offers).ThenInclude(o => o.Supplier)
            .Include(p => p.Ratings)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Aufrufe += 1;
        await db.SaveChangesAsync(cancellationToken);

        var angebote = product.Offers
            .Select(o => new OfferDto(o.SupplierId, o.Supplier!.Name, o.Preis))
            .OrderBy(o => o.Preis)
            .ToList();

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Beschreibung,
            product.Subcategory!.CategoryId,
            product.Subcategory!.Category!.Name,
            product.SubcategoryId,
            product.Subcategory!.Name,
            ParseEigenschaften(product.EigenschaftenJson),
            product.Ratings.Count > 0 ? product.Ratings.Average(r => r.Wert) : null,
            product.Ratings.Count,
            product.Aufrufe,
            angebote);
    }

    public async Task<RatingResponseDto?> AddOrReplaceRatingAsync(string productId, string autorName, int wert, CancellationToken cancellationToken = default)
    {
        var productExists = await db.Products.AnyAsync(p => p.Id == productId, cancellationToken);
        if (!productExists)
        {
            return null;
        }

        var normalizedAutor = autorName.Trim();
        var existing = await db.Ratings
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.AutorName.ToLower() == normalizedAutor.ToLower(), cancellationToken);

        if (existing is not null)
        {
            existing.Wert = wert;
        }
        else
        {
            db.Ratings.Add(new Rating { ProductId = productId, AutorName = normalizedAutor, Wert = wert });
        }

        await db.SaveChangesAsync(cancellationToken);

        var ratings = await db.Ratings.Where(r => r.ProductId == productId).ToListAsync(cancellationToken);
        return new RatingResponseDto(ratings.Average(r => r.Wert), ratings.Count);
    }

    private static Dictionary<string, JsonElement> ParseEigenschaften(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];

    private static string NormalizeJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "",
        _ => element.GetRawText()
    };

    private sealed record ProductComputed(
        Product Product,
        Dictionary<string, JsonElement> Eigenschaften,
        decimal? MinPreis,
        double? DurchschnittsBewertung,
        int AnzahlBewertungen);
}
