namespace seed_b_backend.Api.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    // Snapshot: the product name may change after the order was placed.
    public string ProductName { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    // Snapshot, same reasoning as ProductName.
    public string SupplierName { get; set; } = string.Empty;

    // Price frozen at order time; never changes afterwards (B-F10).
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
