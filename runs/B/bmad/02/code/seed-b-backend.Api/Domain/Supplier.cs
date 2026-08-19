namespace seed_b_backend.Api.Domain;

public class Supplier
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
