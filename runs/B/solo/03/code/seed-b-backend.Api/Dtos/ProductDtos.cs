namespace seed_b_backend.Api.Dtos;

public enum ProductSort
{
    NameAsc,
    NameDesc,
    PriceAsc,
    PriceDesc,
    PopularityDesc,
    PopularityAsc,
}

public record ProductListItemDto(
    string Id,
    string Name,
    string SubCategoryId,
    string SubCategoryName,
    string CategoryId,
    string CategoryName,
    decimal MinPrice,
    long ViewCount,
    double AverageRating,
    int RatingCount);

public record ProductListResponseDto(
    List<ProductListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record OfferDto(string SupplierId, string SupplierName, decimal Price);

public record ProductDetailDto(
    string Id,
    string Name,
    string Description,
    string SubCategoryId,
    string SubCategoryName,
    string CategoryId,
    string CategoryName,
    Dictionary<string, string> Properties,
    double AverageRating,
    int RatingCount,
    long ViewCount,
    List<OfferDto> Offers);
