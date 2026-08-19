namespace seed_b_backend.Api.Controllers.Dtos;

public class ProductDetailDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required CategoryRefDto Category { get; set; }
    public required SubcategoryRefDto Subcategory { get; set; }
    public required IReadOnlyList<ProductPropertyDto> Properties { get; set; }
    public double? AverageRating { get; set; }
    public required int RatingCount { get; set; }
    public required IReadOnlyList<SupplierOfferDto> Offers { get; set; }
}
