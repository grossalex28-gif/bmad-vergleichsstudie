namespace seed_b_backend.Api.Dtos;

public class ProductOfferDto
{
    public string SupplierId { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public decimal Price { get; set; }
}
