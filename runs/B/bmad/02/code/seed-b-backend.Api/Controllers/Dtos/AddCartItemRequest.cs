using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Controllers.Dtos;

public class AddCartItemRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string ProductId { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string SupplierId { get; set; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; set; }
}
