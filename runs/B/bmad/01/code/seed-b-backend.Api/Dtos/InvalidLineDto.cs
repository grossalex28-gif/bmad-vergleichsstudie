namespace seed_b_backend.Api.Dtos;

public class InvalidLineDto
{
    public string ProductId { get; set; } = null!;
    public string SupplierId { get; set; } = null!;
    public string Reason { get; set; } = null!;
}
