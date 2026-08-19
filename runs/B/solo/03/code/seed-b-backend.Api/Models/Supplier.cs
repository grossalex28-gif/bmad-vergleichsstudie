namespace seed_b_backend.Api.Models;

public class Supplier
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public List<Offer> Offers { get; set; } = [];
}
