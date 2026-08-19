namespace seed_b_backend.Api.Models;

public class Rating
{
    public int Id { get; set; }
    public required string ProductId { get; set; }
    public required string AutorName { get; set; }
    public required int Wert { get; set; }

    public Product? Product { get; set; }
}
