namespace seed_b_backend.Api.Application;

public class CartResult
{
    public required Guid? CartId { get; set; }
    public required List<CartResultLine> Items { get; set; }
    public required decimal TotalPrice { get; set; }
}
