namespace seed_b_backend.Api.Controllers.Dtos;

public class ProductListResponse
{
    public required IReadOnlyList<ProductSummaryDto> Items { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
}
