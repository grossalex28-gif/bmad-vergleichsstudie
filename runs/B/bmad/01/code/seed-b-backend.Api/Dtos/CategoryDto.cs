namespace seed_b_backend.Api.Dtos;

public class CategoryDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<SubcategoryDto> Subcategories { get; set; } = [];
}
