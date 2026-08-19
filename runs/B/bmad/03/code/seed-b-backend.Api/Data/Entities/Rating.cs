namespace seed_b_backend.Api.Data.Entities;

public class Rating
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Value { get; set; }

    public Product? Product { get; set; }
}
