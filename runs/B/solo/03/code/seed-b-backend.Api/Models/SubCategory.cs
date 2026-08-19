namespace seed_b_backend.Api.Models;

public class SubCategory
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string CategoryId { get; set; }

    public Category? Category { get; set; }

    // Names of the category-specific properties products in this subcategory can carry.
    public List<string> PropertyNames { get; set; } = [];

    public List<Product> Products { get; set; } = [];
}
