namespace seed_a_backend.Api.Domain;

public class PriceCategory
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public required string Name { get; set; }
    public decimal Preis { get; set; }
}
