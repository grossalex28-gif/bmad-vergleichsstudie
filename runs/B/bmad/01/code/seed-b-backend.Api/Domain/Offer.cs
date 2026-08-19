namespace seed_b_backend.Api.Domain;

public class Offer
{
    public int Id { get; set; }
    public string ProductId { get; set; } = null!;
    public string SupplierId { get; set; } = null!;
    public decimal Price { get; set; }

    public Product Product { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
