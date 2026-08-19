namespace seed_b_backend.Api.Domain;

public class Cart
{
    public Guid Id { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
