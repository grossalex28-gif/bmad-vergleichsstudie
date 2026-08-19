using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Data;

public static class DataSeeder
{
    public static async Task SeedIfEmptyAsync(ShopDbContext db, string seedFilePath, ILogger logger)
    {
        await db.Database.MigrateAsync();

        if (await db.Categories.AnyAsync())
        {
            return;
        }

        if (!File.Exists(seedFilePath))
        {
            logger.LogWarning("Seed-Datei {Path} nicht gefunden, überspringe Initialbefüllung.", seedFilePath);
            return;
        }

        logger.LogInformation("Lade Anfangsdatenbestand aus {Path}.", seedFilePath);

        await using var stream = File.OpenRead(seedFilePath);
        var root = await JsonSerializer.DeserializeAsync<SeedRoot>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Anfangsdatenbestand konnte nicht gelesen werden.");

        foreach (var seedCategory in root.Kategorien)
        {
            var category = new Category { Id = seedCategory.Id, Name = seedCategory.Name };
            foreach (var seedSub in seedCategory.Unterkategorien)
            {
                category.SubCategories.Add(new SubCategory
                {
                    Id = seedSub.Id,
                    Name = seedSub.Name,
                    CategoryId = category.Id,
                    PropertyNames = [.. seedSub.Eigenschaften],
                });
            }
            db.Categories.Add(category);
        }

        foreach (var seedSupplier in root.Lieferanten)
        {
            db.Suppliers.Add(new Supplier { Id = seedSupplier.Id, Name = seedSupplier.Name });
        }

        foreach (var seedProduct in root.Produkte)
        {
            var product = new Product
            {
                Id = seedProduct.Id,
                Name = seedProduct.Name,
                Description = seedProduct.Beschreibung,
                SubCategoryId = seedProduct.UnterkategorieId,
                ViewCount = 0,
            };

            foreach (var (name, value) in seedProduct.Eigenschaften)
            {
                var stringValue = JsonElementToString(value);
                if (stringValue is null)
                {
                    continue;
                }

                product.Properties.Add(new ProductProperty
                {
                    ProductId = product.Id,
                    Name = name,
                    Value = stringValue,
                });
            }

            foreach (var seedOffer in seedProduct.Angebote)
            {
                product.Offers.Add(new Offer
                {
                    ProductId = product.Id,
                    SupplierId = seedOffer.LieferantId,
                    Price = seedOffer.Preis,
                });
            }

            db.Products.Add(product);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Anfangsdatenbestand geladen: {Categories} Kategorien, {Products} Produkte, {Suppliers} Lieferanten.",
            root.Kategorien.Count, root.Produkte.Count, root.Lieferanten.Count);
    }

    private static string? JsonElementToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        _ => value.GetRawText(),
    };
}
