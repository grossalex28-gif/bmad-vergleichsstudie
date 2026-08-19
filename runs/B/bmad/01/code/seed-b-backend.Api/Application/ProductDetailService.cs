using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class ProductDetailService(AppDbContext db)
{
    public async Task<ProductDetailDto?> GetProductDetailAsync(string id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await db.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);

        if (rowsAffected == 0)
        {
            return null;
        }

        return await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.Subcategory.CategoryId,
                CategoryName = p.Subcategory.Category.Name,
                SubcategoryId = p.SubcategoryId,
                SubcategoryName = p.Subcategory.Name,
                Properties = p.ProductProperties
                    .OrderBy(pp => pp.PropertyDefinition.Name)
                    .Select(pp => new ProductPropertyDto
                    {
                        Name = pp.PropertyDefinition.Name,
                        Value = pp.Value
                    })
                    .ToList(),
                Offers = p.Offers
                    .OrderBy(o => o.SupplierId)
                    .Select(o => new ProductOfferDto
                    {
                        SupplierId = o.SupplierId,
                        SupplierName = o.Supplier.Name,
                        Price = o.Price
                    })
                    .ToList(),
                AverageRating = p.Ratings.Select(r => (decimal?)r.Value).Average(),
                RatingCount = p.Ratings.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
