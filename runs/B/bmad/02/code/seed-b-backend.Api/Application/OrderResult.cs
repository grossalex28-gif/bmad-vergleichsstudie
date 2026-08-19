namespace seed_b_backend.Api.Application;

public class OrderResult
{
    public required Guid OrderId { get; set; }
    public required string Status { get; set; }
    public required List<OrderResultLine> Items { get; set; }
    public required decimal TotalPrice { get; set; }
}
