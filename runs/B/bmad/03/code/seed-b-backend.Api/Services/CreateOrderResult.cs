namespace seed_b_backend.Api.Services;

public sealed record CreateOrderResult(Guid? OrderId, string? RejectionReason, string? RejectionDetail)
{
    public static CreateOrderResult Success(Guid orderId) => new(orderId, null, null);
    public static CreateOrderResult Rejected(string reason, string detail) => new(null, reason, detail);
}
