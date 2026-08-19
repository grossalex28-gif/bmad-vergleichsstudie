namespace seed_a_backend.Api.Infrastructure.SeedModels;

public class SeedDataDto
{
    public required List<SeedVenueDto> Spielstaetten { get; set; } = new();
    public required List<SeedEventDto> Veranstaltungen { get; set; } = new();
}

public class SeedVenueDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required List<SeedRoomDto> Raeume { get; set; } = new();
}

public class SeedRoomDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required List<string?> Reihen { get; set; } = new();
    public int Spalten { get; set; }
    public SeedAisleDto? Gang { get; set; }
}

public class SeedAisleDto
{
    public List<int> Spalten { get; set; } = new();
    public string? Hinweis { get; set; }
}

public class SeedEventDto
{
    public required string Id { get; set; }
    public required string Titel { get; set; }
    public required string Beschreibung { get; set; }
    public required string SpielstaetteId { get; set; }
    public required string RaumId { get; set; }
    public DateTime Zeitpunkt { get; set; }
    public int DauerMinuten { get; set; }
    public int Altersfreigabe { get; set; }
    public required List<SeedPriceCategoryDto> Preiskategorien { get; set; } = new();
    public required List<string?> BelegteSitzplaetze { get; set; } = new();
}

public class SeedPriceCategoryDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public decimal Preis { get; set; }
}
