namespace seed_b_backend.Api.Data.Entities;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubcategoryId { get; set; } = string.Empty;
    public string Attributes { get; set; } = "{}";
    public int ViewCount { get; set; } = 0;

    public Subcategory? Subcategory { get; set; }
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
