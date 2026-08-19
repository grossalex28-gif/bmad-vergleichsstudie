namespace seed_b_backend.Api.Models;

public class Category
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public List<SubCategory> SubCategories { get; set; } = [];
}
