namespace seed_b_backend.Api.Dtos;

public record CategoryDto(string Id, string Name, List<SubcategoryDto> Unterkategorien);

public record SubcategoryDto(string Id, string Name, List<string> Eigenschaften);
