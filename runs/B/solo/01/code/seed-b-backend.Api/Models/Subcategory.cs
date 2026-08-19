namespace seed_b_backend.Api.Models;

public class Subcategory
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string CategoryId { get; set; }

    public Category? Category { get; set; }

    // Names of the product properties (Eigenschaften) that apply to this subcategory.
    public List<string> Eigenschaften { get; set; } = [];

    public List<Product> Products { get; set; } = [];
}
