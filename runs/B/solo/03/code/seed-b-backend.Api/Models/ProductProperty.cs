namespace seed_b_backend.Api.Models;

// Category-specific product attribute, stored as name/value pair (EAV) so the
// set of properties can differ per subcategory without schema changes.
public class ProductProperty
{
    public int Id { get; set; }
    public required string ProductId { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }

    public Product? Product { get; set; }
}
