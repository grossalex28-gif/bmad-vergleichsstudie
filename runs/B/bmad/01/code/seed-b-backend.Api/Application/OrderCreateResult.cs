using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Application;

public class OrderCreateResult
{
    public bool Success { get; private init; }
    public OrderDto? Order { get; private init; }
    public string? ErrorMessage { get; private init; }
    public List<InvalidLineDto>? InvalidLines { get; private init; }

    public static OrderCreateResult Ok(OrderDto order) => new() { Success = true, Order = order };

    public static OrderCreateResult Invalid(string errorMessage, List<InvalidLineDto>? invalidLines = null) =>
        new() { Success = false, ErrorMessage = errorMessage, InvalidLines = invalidLines };
}
