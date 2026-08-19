namespace seed_b_backend.Api.Application;

public class CartMutationResult
{
    public required CartMutationStatus Status { get; set; }
    public CartResult? Cart { get; set; }
}
