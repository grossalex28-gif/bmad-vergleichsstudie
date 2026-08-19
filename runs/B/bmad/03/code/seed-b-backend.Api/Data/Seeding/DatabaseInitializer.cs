using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data.Entities;

namespace seed_b_backend.Api.Data.Seeding;

public static class DatabaseInitializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task MigrateAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
    }

    public static async Task SeedIfEmptyAsync(AppDbContext db, string seedFilePath)
    {
        if (await db.Products.AnyAsync())
        {
            return;
        }

        await using var stream = File.OpenRead(seedFilePath);
        var seed = await JsonSerializer.DeserializeAsync<SeedRoot>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Anfangsdatenbestand konnte nicht aus '{seedFilePath}' gelesen werden.");

        foreach (var kategorie in seed.Kategorien)
        {
            db.Categories.Add(new Category { Id = kategorie.Id, Name = kategorie.Name });

            foreach (var unterkategorie in kategorie.Unterkategorien)
            {
                db.Subcategories.Add(new Subcategory
                {
                    Id = unterkategorie.Id,
                    Name = unterkategorie.Name,
                    CategoryId = kategorie.Id,
                    AttributeNames = unterkategorie.Eigenschaften
                });
            }
        }

        foreach (var lieferant in seed.Lieferanten)
        {
            db.Suppliers.Add(new Supplier { Id = lieferant.Id, Name = lieferant.Name });
        }

        foreach (var produkt in seed.Produkte)
        {
            db.Products.Add(new Product
            {
                Id = produkt.Id,
                Name = produkt.Name,
                Description = produkt.Beschreibung,
                SubcategoryId = produkt.UnterkategorieId,
                Attributes = produkt.Eigenschaften.GetRawText(),
                ViewCount = 0
            });

            foreach (var angebot in produkt.Angebote)
            {
                db.Offers.Add(new Offer
                {
                    ProductId = produkt.Id,
                    SupplierId = angebot.LieferantId,
                    Price = angebot.Preis
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private sealed record SeedRoot(
        List<SeedKategorie> Kategorien,
        List<SeedLieferant> Lieferanten,
        List<SeedProdukt> Produkte);

    private sealed record SeedKategorie(string Id, string Name, List<SeedUnterkategorie> Unterkategorien);

    private sealed record SeedUnterkategorie(string Id, string Name, List<string> Eigenschaften);

    private sealed record SeedLieferant(string Id, string Name);

    private sealed record SeedProdukt(
        string Id,
        string Name,
        string Beschreibung,
        string UnterkategorieId,
        JsonElement Eigenschaften,
        List<SeedAngebot> Angebote);

    private sealed record SeedAngebot(string LieferantId, decimal Preis);
}
