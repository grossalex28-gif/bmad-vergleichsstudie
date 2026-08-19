namespace seed_a_backend.Api.Domain;

public class Room
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid VenueId { get; set; }
    public Venue? Venue { get; set; }

    public List<string> Reihen { get; set; } = new();
    public int SpaltenAnzahl { get; set; }
    public List<int> GangSpalten { get; set; } = new();
}
