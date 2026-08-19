using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Data;

public static class DataSeeder
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, string webRootOrContentRoot, ILogger logger)
    {
        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var path = Path.Combine(webRootOrContentRoot, "Data", "anfangsdatenbestand.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Anfangsdatenbestand nicht gefunden unter {Path}, überspringe Seeding.", path);
            return;
        }

        var json = await File.ReadAllTextAsync(path);
        var seed = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        var categoriesBySeedId = new Dictionary<string, Category>();
        var suppliersBySeedId = new Dictionary<string, Supplier>();

        foreach (var seedCategory in seed.Kategorien)
        {
            var category = new Category { Name = seedCategory.Name };
            categoriesBySeedId[seedCategory.Id] = category;
            db.Categories.Add(category);

            foreach (var seedSub in seedCategory.Unterkategorien)
            {
                var subcategory = new Category
                {
                    Name = seedSub.Name,
                    ParentCategory = category,
                    PropertyNames = seedSub.Eigenschaften,
                };
                categoriesBySeedId[seedSub.Id] = subcategory;
                db.Categories.Add(subcategory);
            }
        }

        foreach (var seedSupplier in seed.Lieferanten)
        {
            var supplier = new Supplier { Name = seedSupplier.Name };
            suppliersBySeedId[seedSupplier.Id] = supplier;
            db.Suppliers.Add(supplier);
        }

        foreach (var seedProduct in seed.Produkte)
        {
            var subcategory = categoriesBySeedId[seedProduct.UnterkategorieId];

            var properties = new Dictionary<string, string>();
            foreach (var (key, value) in seedProduct.Eigenschaften)
            {
                var stringValue = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => value.GetRawText(),
                };
                if (stringValue is not null)
                {
                    properties[key] = stringValue;
                }
            }

            var product = new Product
            {
                Name = seedProduct.Name,
                Description = seedProduct.Beschreibung,
                Subcategory = subcategory,
                Properties = properties,
                ViewCount = 0,
            };

            foreach (var seedOffer in seedProduct.Angebote)
            {
                product.Offers.Add(new ProductOffer
                {
                    Product = product,
                    Supplier = suppliersBySeedId[seedOffer.LieferantId],
                    Price = seedOffer.Preis,
                });
            }

            db.Products.Add(product);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Anfangsdatenbestand geladen: {Categories} Kategorien, {Suppliers} Lieferanten, {Products} Produkte.",
            categoriesBySeedId.Count, suppliersBySeedId.Count, seed.Produkte.Count);
    }
}
