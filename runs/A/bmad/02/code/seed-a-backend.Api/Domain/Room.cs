namespace seed_a_backend.Api.Domain;

public class Room
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public required string Name { get; set; }

    /// <summary>Reihenbezeichner in Anzeigereihenfolge, 1:1 aus dem Anfangsdatenbestand (AD-8).</summary>
    public IReadOnlyList<string> RowLabels { get; set; } = new List<string>();

    public int ColumnCount { get; set; }

    /// <summary>Spaltennummern, die Gang statt Sitzplatz sind (AD-8), leer wenn kein Gang.</summary>
    public IReadOnlyCollection<int> AisleColumns { get; set; } = new List<int>();

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
