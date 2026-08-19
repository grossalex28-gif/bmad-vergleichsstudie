namespace seed_b_backend.Api.Data.Entities;

public class Supplier
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
