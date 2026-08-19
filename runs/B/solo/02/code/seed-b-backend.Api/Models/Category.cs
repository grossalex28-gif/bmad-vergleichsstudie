namespace seed_b_backend.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public List<Category> Subcategories { get; set; } = [];

    // Names of the category-specific properties products in this subcategory carry.
    // Only meaningful for subcategories (ParentCategoryId != null).
    public List<string> PropertyNames { get; set; } = [];

    public List<Product> Products { get; set; } = [];
}
