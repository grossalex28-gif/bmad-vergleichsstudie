using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data;

public static class SeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public static async Task LoadAsync(AppDbContext context, string contentRootPath)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        var path = Path.Combine(contentRootPath, "Data", "anfangsdatenbestand.json");
        var json = await File.ReadAllTextAsync(path);
        var root = JsonSerializer.Deserialize<SeedRoot>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        foreach (var seedCategory in root.Kategorien)
        {
            var category = new Category { Id = seedCategory.Id, Name = seedCategory.Name };
            context.Categories.Add(category);

            foreach (var seedSubcategory in seedCategory.Unterkategorien)
            {
                var subcategory = new Subcategory
                {
                    Id = seedSubcategory.Id,
                    Name = seedSubcategory.Name,
                    CategoryId = category.Id,
                };
                context.Subcategories.Add(subcategory);

                foreach (var propertyName in seedSubcategory.Eigenschaften)
                {
                    context.SubcategoryProperties.Add(new SubcategoryProperty
                    {
                        SubcategoryId = subcategory.Id,
                        Name = propertyName,
                    });
                }
            }
        }

        foreach (var seedSupplier in root.Lieferanten)
        {
            context.Suppliers.Add(new Supplier { Id = seedSupplier.Id, Name = seedSupplier.Name });
        }

        foreach (var seedProduct in root.Produkte)
        {
            var product = new Product
            {
                Id = seedProduct.Id,
                Name = seedProduct.Name,
                Description = seedProduct.Beschreibung,
                SubcategoryId = seedProduct.UnterkategorieId,
            };
            context.Products.Add(product);

            foreach (var (propertyName, rawValue) in seedProduct.Eigenschaften)
            {
                var canonicalValue = SeedValueCanonicalizer.Canonicalize(rawValue);
                if (canonicalValue is null)
                {
                    continue;
                }

                context.ProductPropertyValues.Add(new ProductPropertyValue
                {
                    ProductId = product.Id,
                    Name = propertyName,
                    Value = canonicalValue,
                });
            }

            foreach (var seedOffer in seedProduct.Angebote)
            {
                context.Offers.Add(new Offer
                {
                    ProductId = product.Id,
                    SupplierId = seedOffer.LieferantId,
                    Price = seedOffer.Preis,
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
