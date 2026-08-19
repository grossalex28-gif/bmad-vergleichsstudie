using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Models;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequestDto request, CancellationToken ct)
    {
        var result = await bookingService.CreateAsync(request, ct);

        return result.Type switch
        {
            CreateBookingResultType.Success => CreatedAtAction(
                nameof(GetByReference),
                new { reference = result.Booking!.Reference },
                ToDto(result.Booking!)),
            CreateBookingResultType.EventNotFound => NotFound(new { message = result.ErrorMessage }),
            CreateBookingResultType.InvalidSeat => BadRequest(new { message = result.ErrorMessage }),
            CreateBookingResultType.Conflict => Conflict(new BookingConflictDto(
                result.ErrorMessage!,
                result.ConflictingSeats.Select(s => new ConflictingSeatDto(s.Row, s.Column)).ToList())),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<BookingDto>> GetByReference(string reference, CancellationToken ct)
    {
        var booking = await bookingService.GetByReferenceAsync(reference, ct);
        return booking is null ? NotFound() : Ok(ToDto(booking));
    }

    [HttpPost("{reference}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(string reference, CancellationToken ct)
    {
        var booking = await bookingService.CancelAsync(reference, ct);
        return booking is null ? NotFound() : Ok(ToDto(booking));
    }

    private static BookingDto ToDto(Booking booking) => new(
        booking.Reference,
        booking.EventId,
        booking.Event.Title,
        booking.Event.StartsAt,
        booking.CustomerName,
        booking.CustomerEmail,
        booking.CreatedAt,
        booking.IsCancelled,
        booking.Seats.Sum(s => s.Price),
        booking.Seats
            .OrderBy(s => s.RowLabel).ThenBy(s => s.Column)
            .Select(s => new BookingSeatDto(s.RowLabel, s.Column, s.PriceCategory.Name, s.Price))
            .ToList());
}
