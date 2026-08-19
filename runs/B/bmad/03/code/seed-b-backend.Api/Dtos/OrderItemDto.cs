namespace seed_b_backend.Api.Dtos;

public record OrderItemDto(string ProductName, string SupplierName, decimal UnitPrice, int Quantity);
