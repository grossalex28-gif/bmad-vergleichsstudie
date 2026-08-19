namespace seed_b_backend.Api.Domain;

public class Category
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;

    public List<Subcategory> Subcategories { get; set; } = [];
}
