namespace seed_a_backend.Api.Models;

public class BookingSeat
{
    public int Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public string EventId { get; set; } = default!;
    public Event Event { get; set; } = default!;

    public string RowLabel { get; set; } = default!;
    public int Column { get; set; }

    public string PriceCategoryId { get; set; } = default!;
    public PriceCategory PriceCategory { get; set; } = default!;

    /// <summary>Price at time of booking, independent of later price category changes.</summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Mirrors Booking.IsCancelled so a filtered unique index can enforce
    /// "no two active bookings for the same seat" without a join.
    /// </summary>
    public bool IsCancelled { get; set; }
}
