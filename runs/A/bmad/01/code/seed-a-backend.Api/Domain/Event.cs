namespace seed_a_backend.Api.Domain;

public class Event
{
    public Guid Id { get; set; }
    public required string Titel { get; set; }
    public required string Beschreibung { get; set; }
    public int DauerMinuten { get; set; }
    public int Altersfreigabe { get; set; }
    public DateTime Zeitpunkt { get; set; }
    public Guid VenueId { get; set; }
    public Venue? Venue { get; set; }
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
}
