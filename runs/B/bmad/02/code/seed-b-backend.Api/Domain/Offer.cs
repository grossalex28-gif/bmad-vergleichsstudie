namespace seed_b_backend.Api.Domain;

public class Offer
{
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public decimal Price { get; set; }

    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
