using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("buchungen")]
public class BuchungenController(IBuchungenService buchungenService) : ControllerBase
{
    [HttpGet("{referenz}")]
    public async Task<ActionResult<BuchungDetailDto>> GetBuchung(string referenz)
    {
        var buchung = await buchungenService.BuchungAbrufenAsync(referenz);
        if (buchung is null)
        {
            return NotFound();
        }

        return Ok(buchung);
    }

    [HttpPost("{referenz}/stornierung")]
    public async Task<ActionResult<BuchungDetailDto>> PostStornierung(string referenz)
    {
        var ergebnis = await buchungenService.BuchungStornierenAsync(referenz);

        return ergebnis.Typ switch
        {
            StornierungErgebnisTyp.Erfolgreich => Ok(ergebnis.Buchung),
            StornierungErgebnisTyp.NichtGefunden => NotFound(),
            StornierungErgebnisTyp.BereitsStorniert => ProblemMitTyp(
                StatusCodes.Status409Conflict,
                "Buchung bereits storniert",
                "bereits_storniert",
                "Diese Buchung wurde bereits storniert."),
            _ => throw new InvalidOperationException($"Unbekannter StornierungErgebnisTyp: {ergebnis.Typ}"),
        };
    }

    private ObjectResult ProblemMitTyp(int status, string titel, string typ, string? detail = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = titel,
            Type = typ,
            Detail = detail,
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
