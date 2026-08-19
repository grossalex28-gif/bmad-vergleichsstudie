namespace seed_a_backend.Api.Models;

public class Raum
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string SpielstaetteId { get; set; } = string.Empty;
    public Spielstaette? Spielstaette { get; set; }

    /// <summary>Reihenbezeichnungen in Anzeigereihenfolge, z. B. ["A","B","C"].</summary>
    public List<string> Reihen { get; set; } = new();

    /// <summary>Anzahl Spalten pro Reihe.</summary>
    public int Spalten { get; set; }

    /// <summary>Spaltennummern (1-basiert), die als Gang statt Sitzplatz gelten.</summary>
    public List<int> GangSpalten { get; set; } = new();

    public string? GangHinweis { get; set; }

    public ICollection<Veranstaltung> Veranstaltungen { get; set; } = new List<Veranstaltung>();
}
