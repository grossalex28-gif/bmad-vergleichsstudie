namespace seed_b_backend.Api.Dtos;

public record ProductDetailDto(
    string Id, string Name, string Description,
    string CategoryId, string CategoryName,
    string SubcategoryId, string SubcategoryName,
    IReadOnlyList<ProductAttributeDto> Attributes,
    double? AverageRating, int RatingCount,
    IReadOnlyList<ProductOfferDto> Offers);
