namespace seed_b_backend.Api.Domain;

public class Product
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string SubcategoryId { get; set; } = null!;
    public int ViewCount { get; set; }

    public Subcategory Subcategory { get; set; } = null!;
    public List<ProductProperty> ProductProperties { get; set; } = [];
    public List<Offer> Offers { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
}
