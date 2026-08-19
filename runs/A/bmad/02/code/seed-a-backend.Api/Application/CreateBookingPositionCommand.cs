namespace seed_a_backend.Api.Application;

public record CreateBookingPositionCommand(string RowLabel, int ColumnNumber, int PriceCategoryId);
