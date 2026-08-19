namespace seed_b_backend.Api.Dtos;

public record CategoryDto(string Id, string Name, IReadOnlyList<SubcategoryDto> Subcategories);
