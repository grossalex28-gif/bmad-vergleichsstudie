namespace seed_a_backend.Api.Domain;

public class PriceCategory
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    public required string Name { get; set; }
    public decimal Preis { get; set; }
    public int Reihenfolge { get; set; }
}
