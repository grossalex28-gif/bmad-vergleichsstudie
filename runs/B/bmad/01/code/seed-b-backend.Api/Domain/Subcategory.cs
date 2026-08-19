namespace seed_b_backend.Api.Domain;

public class Subcategory
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string CategoryId { get; set; } = null!;

    public Category Category { get; set; } = null!;
    public List<PropertyDefinition> PropertyDefinitions { get; set; } = [];
    public List<Product> Products { get; set; } = [];
}
