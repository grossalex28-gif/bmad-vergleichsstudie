namespace seed_b_backend.Api.Models;

public class Review
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
