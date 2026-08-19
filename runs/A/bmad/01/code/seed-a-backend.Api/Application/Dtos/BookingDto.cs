namespace seed_a_backend.Api.Application.Dtos;

public record CreateBookingSeatRequestDto(Guid SeatId, Guid PriceCategoryId);
public record CreateBookingRequestDto(Guid EventId, string? Name, string? Email, List<CreateBookingSeatRequestDto>? Seats);

public record BookingSeatDto(Guid SeatId, string Row, int Column, Guid PriceCategoryId, string PriceCategoryName, decimal Price);
public record BookingDto(string Reference, Guid EventId, string EventTitle, string VenueName, DateTime StartsAt, string Status, List<BookingSeatDto> Seats, decimal TotalPrice);
