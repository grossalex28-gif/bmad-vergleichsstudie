namespace seed_a_backend.Api.Application;

public abstract record BookingCreateResult
{
    public sealed record Success(BookingResult Booking) : BookingCreateResult;

    public sealed record EventNotFound : BookingCreateResult;

    public sealed record ValidationFailed(string Message) : BookingCreateResult;

    public sealed record SeatConflict(IReadOnlyList<string> ConflictingSeats) : BookingCreateResult;
}
