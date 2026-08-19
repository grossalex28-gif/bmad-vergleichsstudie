namespace seed_b_backend.Api.Dtos;

public class ProductListItemDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal? LowestPrice { get; set; }
}
