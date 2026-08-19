namespace seed_b_backend.Api.Controllers.Dtos;

public class OrderRejectionLineDto
{
    public required string ProductId { get; set; }
    public required string SupplierId { get; set; }
    public required string Reason { get; set; }
}
