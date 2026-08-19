namespace seed_b_backend.Api.Models;

public class Rating
{
    public int Id { get; set; }
    public required string ProductId { get; set; }
    public required string AuthorName { get; set; }
    public int Stars { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Product? Product { get; set; }
}
