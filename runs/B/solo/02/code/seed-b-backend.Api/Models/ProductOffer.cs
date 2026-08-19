namespace seed_b_backend.Api.Models;

// An offer of a product by a supplier at a given price.
// The current, mutable price of the item; order positions freeze their own copy.
public class ProductOffer
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public decimal Price { get; set; }
}
