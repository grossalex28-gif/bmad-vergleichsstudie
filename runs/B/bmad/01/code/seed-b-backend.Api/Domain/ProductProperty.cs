namespace seed_b_backend.Api.Domain;

public class ProductProperty
{
    public int Id { get; set; }
    public string ProductId { get; set; } = null!;
    public int PropertyDefinitionId { get; set; }
    public string Value { get; set; } = null!;

    public Product Product { get; set; } = null!;
    public PropertyDefinition PropertyDefinition { get; set; } = null!;
}
