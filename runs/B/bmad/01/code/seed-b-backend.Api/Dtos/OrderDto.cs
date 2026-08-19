namespace seed_b_backend.Api.Dtos;

public class OrderDto
{
    public Guid PublicId { get; set; }
    public string Status { get; set; } = null!;
    public DeliveryDto Delivery { get; set; } = null!;
    public List<OrderItemResponseDto> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
