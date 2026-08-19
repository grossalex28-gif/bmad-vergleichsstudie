namespace seed_b_backend.Api.Domain;

public class CartItem
{
    public required Guid CartId { get; set; }
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public required int Quantity { get; set; }

    public Cart? Cart { get; set; }
    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
