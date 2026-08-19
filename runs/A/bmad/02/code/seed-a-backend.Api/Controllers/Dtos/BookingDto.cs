namespace seed_a_backend.Api.Controllers.Dtos;

public record BookingDto(string Reference, string Name, string Status, IReadOnlyList<BookingPositionDto> Positionen, decimal Gesamtpreis);
