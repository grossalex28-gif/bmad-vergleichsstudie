using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Controllers.Dtos;

public class UpdateCartItemQuantityRequest
{
    [Range(1, int.MaxValue)]
    public required int Quantity { get; set; }
}
