using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace seed_a_backend.Api.Services;

public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Nicht gefunden"),
            ValidationException => (StatusCodes.Status400BadRequest, "Ungültige Anfrage"),
            SeatConflictException => (StatusCodes.Status409Conflict, "Sitzplatzkonflikt"),
            _ => (StatusCodes.Status500InternalServerError, "Unerwarteter Fehler")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "Es ist ein unerwarteter Fehler aufgetreten." : exception.Message
        };

        if (exception is SeatConflictException seatConflict)
        {
            problemDetails.Extensions["belegteSitzplaetze"] = seatConflict.BelegteSitzplaetze
                .Select(s => new { reihe = s.Reihe, spalte = s.Spalte })
                .ToList();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
