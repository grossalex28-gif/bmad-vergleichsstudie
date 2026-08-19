namespace seed_b_backend.Api.Dtos;

public record ProductListItemDto(
    int Id,
    string Name,
    int SubcategoryId,
    string SubcategoryName,
    int CategoryId,
    string CategoryName,
    decimal? MinPrice,
    double? AverageRating,
    int RatingCount,
    long ViewCount);

public record PagedResultDto<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public record ProductOfferDto(int SupplierId, string SupplierName, decimal Price);

public record ProductDetailDto(
    int Id,
    string Name,
    string Description,
    int SubcategoryId,
    string SubcategoryName,
    int CategoryId,
    string CategoryName,
    Dictionary<string, string> Properties,
    double? AverageRating,
    int RatingCount,
    long ViewCount,
    List<ProductOfferDto> Offers);

public record ReviewCreateDto(string AuthorName, int Rating);

public record ReviewResultDto(double AverageRating, int RatingCount);
