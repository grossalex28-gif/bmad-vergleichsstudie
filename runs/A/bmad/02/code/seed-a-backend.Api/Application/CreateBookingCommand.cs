namespace seed_a_backend.Api.Application;

public record CreateBookingCommand(int EventId, string Name, string Email, IReadOnlyList<CreateBookingPositionCommand> Positions);
