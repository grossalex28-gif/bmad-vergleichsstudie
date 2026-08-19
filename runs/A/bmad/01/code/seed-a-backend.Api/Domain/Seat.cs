namespace seed_a_backend.Api.Domain;

public class Seat
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
    public required string Row { get; set; }
    public int Column { get; set; }
}
