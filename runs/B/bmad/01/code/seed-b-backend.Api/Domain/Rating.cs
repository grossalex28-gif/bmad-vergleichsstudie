namespace seed_b_backend.Api.Domain;

public class Rating
{
    public int Id { get; set; }
    public string ProductId { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public int Value { get; set; }

    public Product Product { get; set; } = null!;
}
