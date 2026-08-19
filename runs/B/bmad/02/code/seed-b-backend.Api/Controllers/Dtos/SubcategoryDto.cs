namespace seed_b_backend.Api.Controllers.Dtos;

public class SubcategoryDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyList<string> Properties { get; set; }
}
