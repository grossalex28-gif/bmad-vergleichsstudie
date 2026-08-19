namespace seed_a_backend.Api.Domain;

public class Booking
{
    /// <summary>Interner Primärschlüssel, wird nie über die API exponiert (AD-4).</summary>
    public int Id { get; set; }

    /// <summary>Öffentliche, von Id getrennte Referenz, 8 Zeichen (AD-4).</summary>
    public required string Reference { get; set; }

    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public ICollection<BookingPosition> Positions { get; set; } = new List<BookingPosition>();
}
