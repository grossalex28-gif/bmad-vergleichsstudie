namespace seed_b_backend.Api.Domain;

public class Product
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string SubcategoryId { get; set; }
    public int ViewCount { get; set; } = 0;

    public Subcategory? Subcategory { get; set; }
    public ICollection<ProductPropertyValue> PropertyValues { get; set; } = new List<ProductPropertyValue>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
