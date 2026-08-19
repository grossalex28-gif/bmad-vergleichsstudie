namespace seed_b_backend.Api.Controllers.Dtos;

public class CategoryDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyList<SubcategoryDto> Subcategories { get; set; }
}
