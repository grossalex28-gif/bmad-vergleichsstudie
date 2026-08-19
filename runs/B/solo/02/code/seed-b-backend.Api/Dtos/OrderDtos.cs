namespace seed_b_backend.Api.Dtos;

public record OrderContactDto(string RecipientName, string Street, string PostalCode, string City, string Email);

public record OrderItemCreateDto(int ProductId, int SupplierId, int Quantity);

public record OrderCreateDto(OrderContactDto Contact, List<OrderItemCreateDto> Items);

public record OrderItemResultDto(
    int ProductId,
    string ProductName,
    int SupplierId,
    string SupplierName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record OrderDto(
    int Id,
    string Status,
    DateTimeOffset CreatedAt,
    OrderContactDto Contact,
    List<OrderItemResultDto> Items,
    decimal Total);

public record OrderValidationErrorDto(string Code, string Message, int? ItemIndex);
