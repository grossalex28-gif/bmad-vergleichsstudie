namespace seed_b_backend.Api.Application;

public class OrderPlacementResult
{
    public required OrderPlacementStatus Status { get; set; }
    public List<OrderLineRejection>? RejectedLines { get; set; }
    public OrderResult? Order { get; set; }
}
