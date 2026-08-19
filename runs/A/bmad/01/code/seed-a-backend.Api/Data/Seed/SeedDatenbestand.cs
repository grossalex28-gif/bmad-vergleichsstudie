using System.Text.Json.Serialization;

namespace seed_a_backend.Api.Data.Seed;

public class SeedDatenbestand
{
    [JsonPropertyName("spielstaetten")]
    public List<SeedSpielstaette> Spielstaetten { get; set; } = new();

    [JsonPropertyName("veranstaltungen")]
    public List<SeedVeranstaltung> Veranstaltungen { get; set; } = new();
}

public class SeedSpielstaette
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("raeume")]
    public List<SeedRaum> Raeume { get; set; } = new();
}

public class SeedRaum
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

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

public class SeedVeranstaltung
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("titel")]
    public required string Titel { get; set; }

    [JsonPropertyName("beschreibung")]
    public required string Beschreibung { get; set; }

    [JsonPropertyName("spielstaetteId")]
    public required string SpielstaetteId { get; set; }

    [JsonPropertyName("raumId")]
    public required string RaumId { get; set; }

    [JsonPropertyName("zeitpunkt")]
    public DateTime Zeitpunkt { get; set; }

    [JsonPropertyName("dauerMinuten")]
    public int DauerMinuten { get; set; }

    [JsonPropertyName("altersfreigabe")]
    public int Altersfreigabe { get; set; }

    [JsonPropertyName("preiskategorien")]
    public List<SeedPreiskategorie> Preiskategorien { get; set; } = new();

    [JsonPropertyName("belegteSitzplaetze")]
    public List<string> BelegteSitzplaetze { get; set; } = new();
}

public class SeedPreiskategorie
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
