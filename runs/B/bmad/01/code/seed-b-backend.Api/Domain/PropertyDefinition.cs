namespace seed_b_backend.Api.Domain;

public class PropertyDefinition
{
    public int Id { get; set; }
    public string SubcategoryId { get; set; } = null!;
    public string Name { get; set; } = null!;

    public Subcategory Subcategory { get; set; } = null!;
    public List<ProductProperty> ProductProperties { get; set; } = [];
}
