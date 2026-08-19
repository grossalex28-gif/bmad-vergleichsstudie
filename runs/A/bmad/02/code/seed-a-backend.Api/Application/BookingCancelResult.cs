namespace seed_a_backend.Api.Application;

public abstract record BookingCancelResult
{
    public sealed record Success(BookingResult Booking) : BookingCancelResult;

    public sealed record NotFound : BookingCancelResult;

    public sealed record AlreadyCancelled : BookingCancelResult;
}
