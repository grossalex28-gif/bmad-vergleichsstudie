using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public record OrderCreationResult(bool Success, string? Error, OrderResponseDto? Order)
{
    public static OrderCreationResult Fail(string error) => new(false, error, null);
    public static OrderCreationResult Ok(OrderResponseDto order) => new(true, null, order);
}

public interface IOrderService
{
    Task<OrderCreationResult> CreateOrderAsync(OrderCreateDto dto, CancellationToken cancellationToken = default);

    Task<OrderResponseDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
