namespace seed_b_backend.Api.Dtos;

public class ProductDetailDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string CategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string SubcategoryId { get; set; } = null!;
    public string SubcategoryName { get; set; } = null!;
    public List<ProductPropertyDto> Properties { get; set; } = [];
    public List<ProductOfferDto> Offers { get; set; } = [];
    public decimal? AverageRating { get; set; }
    public int RatingCount { get; set; }
}
