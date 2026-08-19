namespace seed_b_backend.Api.Controllers.Dtos;

public class OrderRejectionDto
{
    public required string Error { get; set; }
    public required List<OrderRejectionLineDto> Lines { get; set; }
}
