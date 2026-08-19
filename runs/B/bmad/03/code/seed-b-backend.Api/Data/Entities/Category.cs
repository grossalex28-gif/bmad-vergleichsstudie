namespace seed_b_backend.Api.Data.Entities;

public class Category
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
