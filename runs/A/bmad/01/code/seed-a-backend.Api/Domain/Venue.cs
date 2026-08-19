namespace seed_a_backend.Api.Domain;

public class Venue
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
