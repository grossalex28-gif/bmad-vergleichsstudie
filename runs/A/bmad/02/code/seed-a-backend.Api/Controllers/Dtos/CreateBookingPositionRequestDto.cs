namespace seed_a_backend.Api.Controllers.Dtos;

public record CreateBookingPositionRequestDto(string RowLabel, int ColumnNumber, int PriceCategoryId);
