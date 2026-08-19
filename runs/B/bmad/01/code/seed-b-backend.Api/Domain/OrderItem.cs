namespace seed_b_backend.Api.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductId { get; set; } = null!;
    public string SupplierId { get; set; } = null!;
    public decimal UnitPriceAtOrder { get; set; }
    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
