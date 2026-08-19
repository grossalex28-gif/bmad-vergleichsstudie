namespace seed_a_backend.Api.Controllers.Dtos;

public record CreateBookingRequestDto(int EventId, string Name, string Email, IReadOnlyList<CreateBookingPositionRequestDto> Positionen);
