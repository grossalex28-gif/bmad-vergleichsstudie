namespace seed_b_backend.Api.Dtos;

public record OrderLineRequestDto(string ProductId, string SupplierId, int Quantity);
