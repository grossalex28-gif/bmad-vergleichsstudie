using System.Text.Json.Serialization;

namespace seed_a_backend.Api.Data;

public class SeedRoot
{
    [JsonPropertyName("spielstaetten")]
    public List<SeedVenue> Spielstaetten { get; set; } = new();

    [JsonPropertyName("veranstaltungen")]
    public List<SeedEvent> Veranstaltungen { get; set; } = new();
}

public class SeedVenue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("raeume")]
    public List<SeedRoom> Raeume { get; set; } = new();
}

public class SeedRoom
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("reihen")]
    public List<string> Reihen { get; set; } = new();

    [JsonPropertyName("spalten")]
    public int Spalten { get; set; }

    [JsonPropertyName("gang")]
    public SeedGang? Gang { get; set; }
}

public class SeedGang
{
    [JsonPropertyName("spalten")]
    public List<int> Spalten { get; set; } = new();

    [JsonPropertyName("hinweis")]
    public string? Hinweis { get; set; }
}

public class SeedEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("titel")]
    public string Titel { get; set; } = default!;

    [JsonPropertyName("beschreibung")]
    public string Beschreibung { get; set; } = default!;

    [JsonPropertyName("spielstaetteId")]
    public string SpielstaetteId { get; set; } = default!;

    [JsonPropertyName("raumId")]
    public string RaumId { get; set; } = default!;

    [JsonPropertyName("zeitpunkt")]
    public DateTime Zeitpunkt { get; set; }

    [JsonPropertyName("dauerMinuten")]
    public int DauerMinuten { get; set; }

    [JsonPropertyName("altersfreigabe")]
    public int Altersfreigabe { get; set; }

    [JsonPropertyName("preiskategorien")]
    public List<SeedPriceCategory> Preiskategorien { get; set; } = new();

    [JsonPropertyName("belegteSitzplaetze")]
    public List<string> BelegteSitzplaetze { get; set; } = new();
}

public class SeedPriceCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
