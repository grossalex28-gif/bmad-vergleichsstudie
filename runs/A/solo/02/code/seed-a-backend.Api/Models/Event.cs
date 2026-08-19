namespace seed_a_backend.Api.Models;

public class Event
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;

    public string VenueId { get; set; } = default!;
    public Venue Venue { get; set; } = default!;

    public string RoomId { get; set; } = default!;
    public Room Room { get; set; } = default!;

    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int AgeRating { get; set; }

    public List<PriceCategory> PriceCategories { get; set; } = new();
    public List<BookingSeat> BookingSeats { get; set; } = new();
}
