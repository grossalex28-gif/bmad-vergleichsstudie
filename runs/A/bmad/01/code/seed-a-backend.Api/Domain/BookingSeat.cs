namespace seed_a_backend.Api.Domain;

public class BookingSeat
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }
    public Seat? Seat { get; set; }
    public Guid PriceCategoryId { get; set; }
    public PriceCategory? PriceCategory { get; set; }
    public bool IsActive { get; set; } = true;
}
