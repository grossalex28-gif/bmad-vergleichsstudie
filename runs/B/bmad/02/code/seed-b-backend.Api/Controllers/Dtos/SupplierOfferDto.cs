namespace seed_b_backend.Api.Controllers.Dtos;

public class SupplierOfferDto
{
    public required string SupplierId { get; set; }
    public required string SupplierName { get; set; }
    public required decimal Price { get; set; }
}
