using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public record CreateOrderItemRequest(
    [param: Required] string ProductId,
    [param: Required] string SupplierId,
    [param: Range(1, 999)] int Quantity);

public record CreateOrderRequest(
    [param: Required, MaxLength(200)] string ContactName,
    [param: Required, EmailAddress, MaxLength(200)] string Email,
    [param: Required, MaxLength(200)] string Street,
    [param: Required, MaxLength(20)] string PostalCode,
    [param: Required, MaxLength(100)] string City,
    [param: Required, MinLength(1)] List<CreateOrderItemRequest> Items);

public record OrderItemDto(
    string ProductId,
    string ProductName,
    string SupplierId,
    string SupplierName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record OrderDto(
    int Id,
    string Status,
    DateTimeOffset CreatedAt,
    string ContactName,
    string Email,
    string Street,
    string PostalCode,
    string City,
    List<OrderItemDto> Items,
    decimal Total);
