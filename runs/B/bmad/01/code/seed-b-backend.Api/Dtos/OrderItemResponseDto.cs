namespace seed_b_backend.Api.Dtos;

public class OrderItemResponseDto
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string SupplierId { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
