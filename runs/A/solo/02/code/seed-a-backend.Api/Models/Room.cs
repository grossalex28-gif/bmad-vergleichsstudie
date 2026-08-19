namespace seed_a_backend.Api.Models;

public class Room
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string VenueId { get; set; } = default!;
    public Venue Venue { get; set; } = default!;

    public List<string> RowLabels { get; set; } = new();
    public int Columns { get; set; }

    /// <summary>1-based column numbers that are aisles rather than seats.</summary>
    public List<int> AisleColumns { get; set; } = new();

    public List<Event> Events { get; set; } = new();
}
