using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class ProductService(AppDbContext db)
{
    public async Task<ProductDetailDto?> GetProductDetailAsync(string id)
    {
        var rowsAffected = await db.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

        if (rowsAffected == 0)
        {
            return null; // Produkt existiert nicht -> Controller mappt auf 404, kein Increment fand statt
        }

        var product = await db.Products
            .Include(p => p.Subcategory!)
                .ThenInclude(s => s!.Category)
            .Include(p => p.Offers)
                .ThenInclude(o => o.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return null;
        }

        var attributes = (product.Subcategory?.AttributeNames ?? [])
            .Select(name => new { name, value = ProductAttributeJson.TryFormatValue(product.Attributes, name) })
            .Where(a => a.value is not null)
            .Select(a => new ProductAttributeDto(a.name, a.value!))
            .ToList();

        var ratingValues = await db.Ratings
            .Where(r => r.ProductId == id)
            .Select(r => r.Value)
            .ToListAsync();

        double? averageRating = ratingValues.Count == 0
            ? null
            : Math.Round(ratingValues.Average(), 1, MidpointRounding.AwayFromZero);

        var offers = product.Offers
            .OrderBy(o => o.Price)
            .ThenBy(o => o.SupplierId)
            .Select(o => new ProductOfferDto(o.SupplierId, o.Supplier!.Name, o.Price))
            .ToList();

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.Subcategory!.CategoryId,
            product.Subcategory.Category!.Name,
            product.SubcategoryId,
            product.Subcategory.Name,
            attributes,
            averageRating,
            ratingValues.Count,
            offers);
    }
}
