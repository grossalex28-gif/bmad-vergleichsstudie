namespace seed_b_backend.Api.Dtos;

public record SubcategoryDto(int Id, string Name, List<string> PropertyNames);

public record CategoryDto(int Id, string Name, List<SubcategoryDto> Subcategories);
