namespace seed_b_backend.Api.Models;

// Bestellposition: Lieferant und Preis werden zum Bestellzeitpunkt festgeschrieben
// (Snapshot) und ändern sich danach nicht mehr, selbst wenn sich Angebot oder Produkt ändern.
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public required string ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string SupplierId { get; set; }
    public required string SupplierName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public Order? Order { get; set; }
}
