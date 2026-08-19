namespace seed_b_backend.Api.Domain;

public class Category
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
