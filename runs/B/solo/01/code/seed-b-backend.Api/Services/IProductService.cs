using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductDetailAsync(string productId, CancellationToken cancellationToken = default);

    Task<RatingResponseDto?> AddOrReplaceRatingAsync(string productId, string autorName, int wert, CancellationToken cancellationToken = default);
}
