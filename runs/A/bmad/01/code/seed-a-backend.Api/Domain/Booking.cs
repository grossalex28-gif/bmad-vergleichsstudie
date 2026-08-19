namespace seed_a_backend.Api.Domain;

public enum BookingStatus
{
    Active,
    Cancelled
}

public class Booking
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public BookingStatus Status { get; set; }
    public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
}
