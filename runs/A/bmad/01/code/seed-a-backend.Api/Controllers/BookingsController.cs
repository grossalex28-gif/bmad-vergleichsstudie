using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(CreateBookingRequestDto request)
    {
        var result = await bookingService.CreateBookingAsync(request);

        if (result.InvalidFields is { Count: > 0 })
        {
            var errors = result.InvalidFields.ToDictionary(f => f, f => new[] { result.ValidationDetail ?? "Ungültige Eingabe." });
            return ValidationProblem(new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest });
        }

        if (result.ConflictingSeats is not null)
        {
            return Problem(
                title: "Sitzplatzkonflikt",
                detail: "Mindestens ein ausgewählter Sitzplatz ist inzwischen vergeben.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["conflictingSeats"] = result.ConflictingSeats });
        }

        return StatusCode(StatusCodes.Status201Created, result.Booking);
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<BookingDto>> GetBooking(string reference)
    {
        var booking = await bookingService.GetBookingByReferenceAsync(reference);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("{reference}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(string reference)
    {
        var result = await bookingService.CancelBookingAsync(reference);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.AlreadyCancelled)
        {
            return Problem(
                title: "Bereits storniert",
                detail: "Diese Buchung wurde bereits storniert.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(result.Booking);
    }
}
