namespace seed_b_backend.Api.Data.Entities;

public class Offer
{
    public string ProductId { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
