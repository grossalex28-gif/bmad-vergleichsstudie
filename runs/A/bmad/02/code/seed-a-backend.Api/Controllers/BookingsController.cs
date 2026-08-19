using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Controllers.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(CreateBookingRequestDto request, CancellationToken ct)
    {
        if (request.Positionen is null || request.Positionen.Any(p => p is null))
        {
            return BadRequest(new ErrorEnvelopeDto(
                "VALIDATION_ERROR", "Die Anfrage enthält ungültige oder unvollständige Daten."));
        }

        var command = new CreateBookingCommand(
            request.EventId, request.Name, request.Email,
            request.Positionen
                .Select(p => new CreateBookingPositionCommand(p.RowLabel, p.ColumnNumber, p.PriceCategoryId))
                .ToList());

        var result = await bookingService.CreateBookingAsync(command, ct);

        return result switch
        {
            BookingCreateResult.Success s => Created($"/api/bookings/{s.Booking.Reference}", ToDto(s.Booking)),
            BookingCreateResult.EventNotFound => NotFound(new ErrorEnvelopeDto("EVENT_NOT_FOUND", "Veranstaltung nicht gefunden.")),
            BookingCreateResult.ValidationFailed v => BadRequest(new ErrorEnvelopeDto("VALIDATION_ERROR", v.Message)),
            BookingCreateResult.SeatConflict c => Conflict(new ErrorEnvelopeDto(
                "SEAT_CONFLICT", "Mindestens ein gewählter Sitzplatz ist inzwischen belegt.", c.ConflictingSeats)),
            _ => throw new InvalidOperationException("Unbekanntes BookingCreateResult."),
        };
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<BookingDto>> GetBooking(string reference, CancellationToken ct)
    {
        var result = await bookingService.GetBookingByReferenceAsync(reference, ct);

        return result is null
            ? NotFound(new ErrorEnvelopeDto("BOOKING_NOT_FOUND", "Buchung nicht gefunden."))
            : Ok(ToDto(result));
    }

    [HttpPost("{reference}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(string reference, CancellationToken ct)
    {
        var result = await bookingService.CancelBookingAsync(reference, ct);

        return result switch
        {
            BookingCancelResult.Success s => Ok(ToDto(s.Booking)),
            BookingCancelResult.NotFound => NotFound(new ErrorEnvelopeDto("BOOKING_NOT_FOUND", "Buchung nicht gefunden.")),
            BookingCancelResult.AlreadyCancelled => Conflict(new ErrorEnvelopeDto(
                "ALREADY_CANCELLED", "Diese Buchung wurde bereits storniert.")),
            _ => throw new InvalidOperationException("Unbekanntes BookingCancelResult."),
        };
    }

    private static BookingDto ToDto(BookingResult result) => new(
        result.Reference, result.Name, result.Status,
        result.Positionen
            .Select(p => new BookingPositionDto(
                p.RowLabel, p.ColumnNumber,
                new PriceCategoryDto(p.Preiskategorie.Id, p.Preiskategorie.Name, p.Preiskategorie.Preis)))
            .ToList(),
        result.Gesamtpreis);
}
