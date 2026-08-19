namespace seed_b_backend.Api.Domain;

public enum OrderStatus
{
    Received,
}

public class Order
{
    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Received;
    public string DeliveryName { get; set; } = null!;
    public string DeliveryStreet { get; set; } = null!;
    public string DeliveryPostalCode { get; set; } = null!;
    public string DeliveryCity { get; set; } = null!;
    public string DeliveryCountry { get; set; } = null!;
    public string DeliveryEmail { get; set; } = null!;
    public string? DeliveryPhone { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
