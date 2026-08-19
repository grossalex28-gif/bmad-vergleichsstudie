using System.Text.Json;
using System.Text.Json.Serialization;

namespace seed_b_backend.Api.Data;

// Modelle für die Datei anfangsdatenbestand.json (Rohformat des initialen Datenbestands).
public class SeedRoot
{
    [JsonPropertyName("kategorien")]
    public List<SeedCategory> Kategorien { get; set; } = [];

    [JsonPropertyName("lieferanten")]
    public List<SeedSupplier> Lieferanten { get; set; } = [];

    [JsonPropertyName("produkte")]
    public List<SeedProduct> Produkte { get; set; } = [];
}

public class SeedCategory
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("unterkategorien")]
    public List<SeedSubCategory> Unterkategorien { get; set; } = [];
}

public class SeedSubCategory
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("eigenschaften")]
    public List<string> Eigenschaften { get; set; } = [];
}

public class SeedSupplier
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class SeedProduct
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("beschreibung")]
    public required string Beschreibung { get; set; }

    [JsonPropertyName("unterkategorieId")]
    public required string UnterkategorieId { get; set; }

    [JsonPropertyName("eigenschaften")]
    public Dictionary<string, JsonElement> Eigenschaften { get; set; } = [];

    [JsonPropertyName("angebote")]
    public List<SeedOffer> Angebote { get; set; } = [];
}

public class SeedOffer
{
    [JsonPropertyName("lieferantId")]
    public required string LieferantId { get; set; }

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
