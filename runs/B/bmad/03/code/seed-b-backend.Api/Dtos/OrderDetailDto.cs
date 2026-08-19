namespace seed_b_backend.Api.Dtos;

public record OrderDetailDto(Guid Id, string Status, IReadOnlyList<OrderItemDto> Items, decimal TotalAmount);
