namespace seed_b_backend.Api.Application;

public class OrderResultLine
{
    public required string ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string SupplierId { get; set; }
    public required string SupplierName { get; set; }
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
}
