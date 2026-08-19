using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Data;

public static class SeedLoader
{
    public static async Task SeedAsync(AppDbContext db, string seedFilePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(seedFilePath, cancellationToken);
        var seed = JsonSerializer.Deserialize<SeedFile>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Seed-Datei '{seedFilePath}' konnte nicht gelesen werden.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var propertyDefinitions = new Dictionary<(string SubcategoryId, string Name), PropertyDefinition>();

        foreach (var kategorie in seed.Kategorien)
        {
            var category = new Category { Id = kategorie.Id, Name = kategorie.Name };
            db.Categories.Add(category);

            foreach (var unterkategorie in kategorie.Unterkategorien)
            {
                var subcategory = new Subcategory
                {
                    Id = unterkategorie.Id,
                    Name = unterkategorie.Name,
                    Category = category
                };
                db.Subcategories.Add(subcategory);

                foreach (var eigenschaftName in unterkategorie.Eigenschaften)
                {
                    if (propertyDefinitions.ContainsKey((unterkategorie.Id, eigenschaftName)))
                    {
                        continue;
                    }

                    var propertyDefinition = new PropertyDefinition
                    {
                        Subcategory = subcategory,
                        Name = eigenschaftName
                    };
                    db.PropertyDefinitions.Add(propertyDefinition);
                    propertyDefinitions[(unterkategorie.Id, eigenschaftName)] = propertyDefinition;
                }
            }
        }

        var suppliers = new Dictionary<string, Supplier>();
        foreach (var lieferant in seed.Lieferanten)
        {
            var supplier = new Supplier { Id = lieferant.Id, Name = lieferant.Name };
            db.Suppliers.Add(supplier);
            suppliers[lieferant.Id] = supplier;
        }

        foreach (var produkt in seed.Produkte)
        {
            var product = new Product
            {
                Id = produkt.Id,
                Name = produkt.Name,
                Description = produkt.Beschreibung,
                SubcategoryId = produkt.UnterkategorieId
            };
            db.Products.Add(product);

            foreach (var (eigenschaftName, wert) in produkt.Eigenschaften)
            {
                var canonicalValue = ToCanonicalValue(wert);
                if (canonicalValue is null)
                {
                    continue;
                }

                if (!propertyDefinitions.TryGetValue((produkt.UnterkategorieId, eigenschaftName), out var propertyDefinition))
                {
                    throw new InvalidOperationException(
                        $"Seed-Datei ist inkonsistent: Produkt '{produkt.Id}' referenziert die Eigenschaft '{eigenschaftName}', die für Unterkategorie '{produkt.UnterkategorieId}' nicht definiert ist.");
                }

                db.ProductProperties.Add(new ProductProperty
                {
                    Product = product,
                    PropertyDefinition = propertyDefinition,
                    Value = canonicalValue
                });
            }

            foreach (var angebot in produkt.Angebote)
            {
                if (!suppliers.TryGetValue(angebot.LieferantId, out var supplier))
                {
                    throw new InvalidOperationException(
                        $"Seed-Datei ist inkonsistent: Produkt '{produkt.Id}' referenziert den unbekannten Lieferanten '{angebot.LieferantId}'.");
                }

                db.Offers.Add(new Offer
                {
                    Product = product,
                    Supplier = supplier,
                    Price = angebot.Preis
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string? ToCanonicalValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => value.GetDecimal().ToString(CultureInfo.InvariantCulture),
        JsonValueKind.String => value.GetString(),
        _ => throw new InvalidOperationException($"Unerwarteter Eigenschaftswert-Typ: {value.ValueKind}")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class SeedFile
    {
        [JsonPropertyName("kategorien")]
        public List<SeedKategorie> Kategorien { get; set; } = [];

        [JsonPropertyName("lieferanten")]
        public List<SeedLieferant> Lieferanten { get; set; } = [];

        [JsonPropertyName("produkte")]
        public List<SeedProdukt> Produkte { get; set; } = [];
    }

    private sealed class SeedKategorie
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("unterkategorien")]
        public List<SeedUnterkategorie> Unterkategorien { get; set; } = [];
    }

    private sealed class SeedUnterkategorie
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("eigenschaften")]
        public List<string> Eigenschaften { get; set; } = [];
    }

    private sealed class SeedLieferant
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    private sealed class SeedProdukt
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("beschreibung")]
        public string Beschreibung { get; set; } = null!;

        [JsonPropertyName("unterkategorieId")]
        public string UnterkategorieId { get; set; } = null!;

        [JsonPropertyName("eigenschaften")]
        public Dictionary<string, JsonElement> Eigenschaften { get; set; } = [];

        [JsonPropertyName("angebote")]
        public List<SeedAngebot> Angebote { get; set; } = [];
    }

    private sealed class SeedAngebot
    {
        [JsonPropertyName("lieferantId")]
        public string LieferantId { get; set; } = null!;

        [JsonPropertyName("preis")]
        public decimal Preis { get; set; }
    }
}
