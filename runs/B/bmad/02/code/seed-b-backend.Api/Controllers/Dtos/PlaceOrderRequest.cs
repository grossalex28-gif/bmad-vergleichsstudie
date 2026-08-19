using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Controllers.Dtos;

public class PlaceOrderRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string CustomerName { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string DeliveryAddress { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }
}
