namespace seed_a_backend.Api.Domain;

public class BookingPosition
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public required string RowLabel { get; set; }
    public int ColumnNumber { get; set; }

    /// <summary>Null nur bei Seed-Import-Zeilen (AD-2), sonst immer gemeinsam mit PriceCategoryId gesetzt.</summary>
    public int? BookingId { get; set; }

    /// <summary>Null nur bei Seed-Import-Zeilen (AD-2), sonst immer gemeinsam mit BookingId gesetzt.</summary>
    public int? PriceCategoryId { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
