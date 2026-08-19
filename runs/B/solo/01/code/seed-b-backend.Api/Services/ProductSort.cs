namespace seed_b_backend.Api.Services;

public enum ProductSort
{
    NameAsc,
    NameDesc,
    PriceAsc,
    PriceDesc,
    ViewsAsc,
    ViewsDesc
}

public record ProductQuery(
    string? Search,
    string? CategoryId,
    string? SubcategoryId,
    ProductSort Sort,
    int Page,
    IReadOnlyDictionary<string, string> Eigenschaften);
