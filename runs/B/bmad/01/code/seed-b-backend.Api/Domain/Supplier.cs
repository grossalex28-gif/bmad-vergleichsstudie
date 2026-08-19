namespace seed_b_backend.Api.Domain;

public class Supplier
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;

    public List<Offer> Offers { get; set; } = [];
}
