using System.Text.Json.Serialization;

namespace seed_b_backend.Api.Data;

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
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("unterkategorien")]
    public List<SeedSubcategory> Unterkategorien { get; set; } = [];
}

public class SeedSubcategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("eigenschaften")]
    public List<string> Eigenschaften { get; set; } = [];
}

public class SeedSupplier
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class SeedProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("beschreibung")]
    public string Beschreibung { get; set; } = string.Empty;

    [JsonPropertyName("unterkategorieId")]
    public string UnterkategorieId { get; set; } = string.Empty;

    [JsonPropertyName("eigenschaften")]
    public Dictionary<string, System.Text.Json.JsonElement> Eigenschaften { get; set; } = [];

    [JsonPropertyName("angebote")]
    public List<SeedOffer> Angebote { get; set; } = [];
}

public class SeedOffer
{
    [JsonPropertyName("lieferantId")]
    public string LieferantId { get; set; } = string.Empty;

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
