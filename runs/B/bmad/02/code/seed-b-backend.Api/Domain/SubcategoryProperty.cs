namespace seed_b_backend.Api.Domain;

public class SubcategoryProperty
{
    public required string SubcategoryId { get; set; }
    public required string Name { get; set; }

    public Subcategory? Subcategory { get; set; }
}
