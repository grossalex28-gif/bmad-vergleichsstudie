namespace seed_b_backend.Api.Models;

public static class OrderStatus
{
    // No order status management exists in this application's scope; orders are
    // created with a single fixed status.
    public const string Received = "Eingegangen";
}

public class Order
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = OrderStatus.Received;

    public string RecipientName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = [];
}
