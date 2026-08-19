namespace seed_b_backend.Api.Domain;

public class Rating
{
    public required string ProductId { get; set; }
    public required string AuthorName { get; set; }
    public required int Score { get; set; }

    public Product? Product { get; set; }
}
