using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public class OrderItemCreateDto
{
    [Required] public string ProductId { get; set; } = null!;
    [Required] public string SupplierId { get; set; } = null!;
    public int Quantity { get; set; }
}
