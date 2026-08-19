namespace seed_b_backend.Api.Controllers.Dtos;

public class OrderDto
{
    public required Guid OrderId { get; set; }
    public required string Status { get; set; }
    public required List<OrderItemDto> Items { get; set; }
    public required decimal TotalPrice { get; set; }
}
