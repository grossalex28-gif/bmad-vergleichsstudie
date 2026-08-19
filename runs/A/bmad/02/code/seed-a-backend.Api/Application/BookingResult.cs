namespace seed_a_backend.Api.Application;

public record BookingResult(string Reference, string Name, string Status, IReadOnlyList<BookingPositionResult> Positionen, decimal Gesamtpreis);
