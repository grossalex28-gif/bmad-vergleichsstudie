namespace seed_b_backend.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int SubcategoryId { get; set; }
    public Category Subcategory { get; set; } = null!;

    // Persistent view counter, incremented on every detail view (B-F13).
    public long ViewCount { get; set; }

    // Category-specific properties, e.g. { "Bauform": "In-Ear", "Kabellos": "true" }.
    public Dictionary<string, string> Properties { get; set; } = [];

    public List<ProductOffer> Offers { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}
