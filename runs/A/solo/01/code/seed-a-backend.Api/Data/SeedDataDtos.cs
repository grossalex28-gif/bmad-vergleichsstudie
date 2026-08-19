using System.Text.Json.Serialization;

namespace seed_a_backend.Api.Data;

public class AnfangsdatenbestandDto
{
    [JsonPropertyName("spielstaetten")]
    public List<SpielstaetteSeedDto> Spielstaetten { get; set; } = [];

    [JsonPropertyName("veranstaltungen")]
    public List<VeranstaltungSeedDto> Veranstaltungen { get; set; } = [];
}

public class SpielstaetteSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("raeume")]
    public List<RaumSeedDto> Raeume { get; set; } = [];
}

public class RaumSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("reihen")]
    public List<string> Reihen { get; set; } = [];

    [JsonPropertyName("spalten")]
    public int Spalten { get; set; }

    [JsonPropertyName("gang")]
    public GangSeedDto? Gang { get; set; }
}

public class GangSeedDto
{
    [JsonPropertyName("spalten")]
    public List<int> Spalten { get; set; } = [];

    [JsonPropertyName("hinweis")]
    public string? Hinweis { get; set; }
}

public class VeranstaltungSeedDto
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
    public List<PreiskategorieSeedDto> Preiskategorien { get; set; } = [];

    [JsonPropertyName("belegteSitzplaetze")]
    public List<string> BelegteSitzplaetze { get; set; } = [];
}

public class PreiskategorieSeedDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("preis")]
    public decimal Preis { get; set; }
}
