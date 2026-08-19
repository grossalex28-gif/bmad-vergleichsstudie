using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Models;

namespace seed_b_backend.Api.Data;

public static class SeedDataLoader
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, string jsonFilePath, CancellationToken cancellationToken = default)
    {
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var seed = JsonSerializer.Deserialize<SeedFile>(json, options)
            ?? throw new InvalidOperationException($"Anfangsdatenbestand konnte nicht gelesen werden: {jsonFilePath}");

        foreach (var k in seed.Kategorien)
        {
            db.Categories.Add(new Category { Id = k.Id, Name = k.Name });
            foreach (var uk in k.Unterkategorien)
            {
                db.Subcategories.Add(new Subcategory
                {
                    Id = uk.Id,
                    Name = uk.Name,
                    CategoryId = k.Id,
                    Eigenschaften = uk.Eigenschaften
                });
            }
        }

        foreach (var l in seed.Lieferanten)
        {
            db.Suppliers.Add(new Supplier { Id = l.Id, Name = l.Name });
        }

        foreach (var p in seed.Produkte)
        {
            db.Products.Add(new Product
            {
                Id = p.Id,
                Name = p.Name,
                Beschreibung = p.Beschreibung,
                SubcategoryId = p.UnterkategorieId,
                EigenschaftenJson = JsonSerializer.Serialize(p.Eigenschaften, options),
                Aufrufe = 0
            });

            foreach (var a in p.Angebote)
            {
                db.Offers.Add(new Offer { ProductId = p.Id, SupplierId = a.LieferantId, Preis = a.Preis });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed class SeedFile
    {
        public List<SeedKategorie> Kategorien { get; set; } = [];
        public List<SeedLieferant> Lieferanten { get; set; } = [];
        public List<SeedProdukt> Produkte { get; set; } = [];
    }

    private sealed class SeedKategorie
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public List<SeedUnterkategorie> Unterkategorien { get; set; } = [];
    }

    private sealed class SeedUnterkategorie
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public List<string> Eigenschaften { get; set; } = [];
    }

    private sealed class SeedLieferant
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
    }

    private sealed class SeedProdukt
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Beschreibung { get; set; }
        public required string UnterkategorieId { get; set; }
        public Dictionary<string, JsonElement> Eigenschaften { get; set; } = [];
        public List<SeedAngebot> Angebote { get; set; } = [];
    }

    private sealed class SeedAngebot
    {
        public required string LieferantId { get; set; }
        public required decimal Preis { get; set; }
    }
}
