namespace seed_b_backend.Api.Domain;

public class Subcategory
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string CategoryId { get; set; }

    public Category? Category { get; set; }
    public ICollection<SubcategoryProperty> Properties { get; set; } = new List<SubcategoryProperty>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
