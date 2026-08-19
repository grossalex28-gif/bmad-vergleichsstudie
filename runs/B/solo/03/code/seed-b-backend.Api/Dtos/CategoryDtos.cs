namespace seed_b_backend.Api.Dtos;

public record SubCategoryDto(string Id, string Name, List<string> PropertyNames);

public record CategoryDto(string Id, string Name, List<SubCategoryDto> SubCategories);
