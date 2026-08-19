namespace seed_b_backend.Api.Domain;

public class OrderItem
{
    public required Guid OrderId { get; set; }
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
    public Supplier? Supplier { get; set; }
}
