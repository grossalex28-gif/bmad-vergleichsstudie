using System.ComponentModel.DataAnnotations;

namespace seed_a_backend.Api.Dtos;

public record CreateBookingRequestDto(
    [Required] string EventId,
    [Required, MaxLength(200)] string CustomerName,
    [Required, EmailAddress, MaxLength(320)] string CustomerEmail,
    [Required, MinLength(1)] List<CreateBookingSeatDto> Seats
);

public record CreateBookingSeatDto(
    [Required] string Row,
    int Column,
    [Required] string PriceCategoryId
);

public record BookingDto(
    string Reference,
    string EventId,
    string EventTitle,
    DateTime EventStartsAt,
    string CustomerName,
    string CustomerEmail,
    DateTime CreatedAt,
    bool IsCancelled,
    decimal TotalPrice,
    List<BookingSeatDto> Seats
);

public record BookingSeatDto(string Row, int Column, string PriceCategoryName, decimal Price);

public record BookingConflictDto(string Message, List<ConflictingSeatDto> ConflictingSeats);

public record ConflictingSeatDto(string Row, int Column);
