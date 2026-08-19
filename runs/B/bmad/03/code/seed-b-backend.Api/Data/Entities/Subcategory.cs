namespace seed_b_backend.Api.Data.Entities;

public class Subcategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public List<string> AttributeNames { get; set; } = new();

    public Category? Category { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
