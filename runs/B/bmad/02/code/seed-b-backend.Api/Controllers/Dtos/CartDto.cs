namespace seed_b_backend.Api.Controllers.Dtos;

public class CartDto
{
    public required Guid? CartId { get; set; }
    public required List<CartItemDto> Items { get; set; }
    public required decimal TotalPrice { get; set; }
}
