namespace seed_b_backend.Api.Dtos;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
