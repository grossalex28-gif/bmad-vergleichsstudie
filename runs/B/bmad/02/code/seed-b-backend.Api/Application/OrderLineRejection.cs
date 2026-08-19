namespace seed_b_backend.Api.Application;

public class OrderLineRejection
{
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public required OrderLineRejectionReason Reason { get; set; }
}
