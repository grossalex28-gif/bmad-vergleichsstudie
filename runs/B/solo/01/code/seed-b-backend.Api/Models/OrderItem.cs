namespace seed_b_backend.Api.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public required string ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string SupplierId { get; set; }
    public required string SupplierName { get; set; }

    // Price at the time the order was placed. Frozen, independent of later Offer.Preis changes.
    public required decimal Preis { get; set; }
    public required int Menge { get; set; }

    public Order? Order { get; set; }
}
