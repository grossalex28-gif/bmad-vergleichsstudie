namespace seed_b_backend.Api.Dtos;

public class PropertyFilterOptionDto
{
    public string Name { get; set; } = null!;
    public List<string> Values { get; set; } = [];
}
