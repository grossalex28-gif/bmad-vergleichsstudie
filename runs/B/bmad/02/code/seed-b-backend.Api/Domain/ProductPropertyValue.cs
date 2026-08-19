namespace seed_b_backend.Api.Domain;

public class ProductPropertyValue
{
    public required string ProductId { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }

    public Product? Product { get; set; }
}
