namespace seed_a_backend.Api.Domain;

public class Event
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public required string Titel { get; set; }
    public required string Beschreibung { get; set; }
    public DateTime Zeitpunkt { get; set; }
    public int DauerMinuten { get; set; }
    public int Altersfreigabe { get; set; }
    public ICollection<PriceCategory> PriceCategories { get; set; } = new List<PriceCategory>();
    public ICollection<BookingPosition> BookingPositions { get; set; } = new List<BookingPosition>();
}
