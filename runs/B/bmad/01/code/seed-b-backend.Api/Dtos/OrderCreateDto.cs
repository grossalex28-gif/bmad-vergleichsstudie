using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public class OrderCreateDto
{
    [Required] public List<OrderItemCreateDto> Items { get; set; } = [];
    [Required] public DeliveryDto Delivery { get; set; } = null!;
}
