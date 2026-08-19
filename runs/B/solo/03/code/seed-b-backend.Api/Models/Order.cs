namespace seed_b_backend.Api.Models;

public enum OrderStatus
{
    Eingegangen,
}

public class Order
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Eingegangen;

    public required string ContactName { get; set; }
    public required string Email { get; set; }
    public required string Street { get; set; }
    public required string PostalCode { get; set; }
    public required string City { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
