namespace seed_b_backend.Api.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<ProductOffer> Offers { get; set; } = [];
}
