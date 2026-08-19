namespace seed_a_backend.Api.Models;

public class PriceCategory
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }

    public string EventId { get; set; } = default!;
    public Event Event { get; set; } = default!;
}
