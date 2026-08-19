namespace seed_b_backend.Api.Dtos;

public class ProductQuery
{
    public string? CategoryId { get; set; }
    public string? SubCategoryId { get; set; }
    public string? Search { get; set; }
    public ProductSort Sort { get; set; } = ProductSort.NameAsc;
    public int Page { get; set; } = 1;
    public Dictionary<string, string> Properties { get; set; } = [];
}
