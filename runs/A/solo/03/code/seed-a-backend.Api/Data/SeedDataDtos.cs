using System.Text.Json.Serialization;

namespace seed_a_backend.Api.Data;

public class AnfangsdatenbestandDto
{
    [JsonPropertyName("spielstaetten")]
    public List<SpielstaetteSeedDto> Spielstaetten { get; set; } = new();

    [JsonPropertyName("veranstaltungen")]
    public List<VeranstaltungSeedDto> Veranstaltungen { get; set; } = new();
}

public class SpielstaetteSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("raeume")]
    public List<RaumSeedDto> Raeume { get; set; } = new();
}

public class RaumSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("reihen")]
    public List<string> Reihen { get; set; } = new();

    [JsonPropertyName("spalten")]
    public int Spalten { get; set; }

    [JsonPropertyName("gang")]
    public GangSeedDto? Gang { get; set; }
}

public class GangSeedDto
{
    [JsonPropertyName("spalten")]
    public List<int> Spalten { get; set; } = new();

    [JsonPropertyName("hinweis")]
    public string? Hinweis { get; set; }
}

public class VeranstaltungSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("titel")]
    public string Titel { get; set; } = string.Empty;

    [JsonPropertyName("beschreibung")]
    public string Beschreibung { get; set; } = string.Empty;

    [JsonPropertyName("spielstaetteId")]
    public string SpielstaetteId { get; set; } = string.Empty;

    [JsonPropertyName("raumId")]
    public string RaumId { get; set; } = string.Empty;

    [JsonPropertyName("zeitpunkt")]
    public DateTime Zeitpunkt { get; set; }

    [JsonPropertyName("dauerMinuten")]
    public int DauerMinuten { get; set; }

    [JsonPropertyName("altersfreigabe")]
    public int Altersfreigabe { get; set; }

    [JsonPropertyName("preiskategorien")]
    public List<PreiskategorieSeedDto> Preiskategorien { get; set; } = new();

    [JsonPropertyName("belegteSitzplaetze")]
    public List<string> BelegteSitzplaetze { get; set; } = new();
}

public class PreiskategorieSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
