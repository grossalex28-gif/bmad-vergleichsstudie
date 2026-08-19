namespace seed_b_backend.Api.Models;

// Ein Angebot: ein Lieferant bietet ein Produkt zu einem bestimmten Preis an.
public class Offer
{
    public int Id { get; set; }
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public decimal Price { get; set; }

    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
