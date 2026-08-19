namespace seed_a_backend.Api.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;

    public string EventId { get; set; } = default!;
    public Event Event { get; set; } = default!;

    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public bool IsCancelled { get; set; }

    public List<BookingSeat> Seats { get; set; } = new();
}
