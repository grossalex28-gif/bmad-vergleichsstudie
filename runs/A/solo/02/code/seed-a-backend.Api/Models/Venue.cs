namespace seed_a_backend.Api.Models;

public class Venue
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;

    public List<Room> Rooms { get; set; } = new();
}
