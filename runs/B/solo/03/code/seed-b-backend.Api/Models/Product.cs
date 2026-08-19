namespace seed_b_backend.Api.Models;

public class Product
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string SubCategoryId { get; set; }
    public long ViewCount { get; set; }

    public SubCategory? SubCategory { get; set; }
    public List<ProductProperty> Properties { get; set; } = [];
    public List<Offer> Offers { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
}
