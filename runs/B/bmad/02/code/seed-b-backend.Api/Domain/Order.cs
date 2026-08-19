namespace seed_b_backend.Api.Domain;

public class Order
{
    public Guid Id { get; set; }
    public required string CustomerName { get; set; }
    public required string DeliveryAddress { get; set; }
    public required string Email { get; set; }
    public string Status { get; set; } = "Neu";

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
