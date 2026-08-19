namespace seed_b_backend.Api.Models;

// An Angebot: one supplier's current price for one product. Composite key (ProductId, SupplierId).
public class Offer
{
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public required decimal Preis { get; set; }

    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
