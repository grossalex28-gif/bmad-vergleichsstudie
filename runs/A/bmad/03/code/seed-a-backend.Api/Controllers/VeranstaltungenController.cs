using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("veranstaltungen")]
public class VeranstaltungenController(IVeranstaltungenService veranstaltungenService, IBuchungenService buchungenService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeranstaltungListeDto>>> GetVeranstaltungen(
        [FromQuery] DateOnly? von,
        [FromQuery] DateOnly? bis,
        [FromQuery] string? spielstaetteId)
    {
        var veranstaltungen = await veranstaltungenService.GetVeranstaltungenAsync(von, bis, spielstaetteId);
        return Ok(veranstaltungen);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VeranstaltungDetailDto>> GetVeranstaltung(string id)
    {
        var veranstaltung = await veranstaltungenService.GetVeranstaltungAsync(id);
        if (veranstaltung is null)
        {
            return NotFound();
        }

        return Ok(veranstaltung);
    }

    [HttpGet("{id}/sitzplan")]
    public async Task<ActionResult<SitzplanDto>> GetSitzplan(string id)
    {
        var sitzplan = await veranstaltungenService.GetSitzplanAsync(id);
        if (sitzplan is null)
        {
            return NotFound();
        }

        return Ok(sitzplan);
    }

    [HttpPost("{id}/buchungen")]
    public async Task<IActionResult> PostBuchung(string id, [FromBody] BuchungAnlegenRequestDto request)
    {
        var ergebnis = await buchungenService.BuchungAnlegenAsync(id, request);

        return ergebnis.Typ switch
        {
            BuchungErgebnisTyp.Erfolgreich =>
                Created($"/buchungen/{ergebnis.Buchung!.Referenz}", ergebnis.Buchung),
            BuchungErgebnisTyp.VeranstaltungNichtGefunden =>
                NotFound(),
            BuchungErgebnisTyp.PreiskategorieUngueltig =>
                ProblemMitTyp(
                    StatusCodes.Status400BadRequest,
                    "Ungültige Preiskategorie",
                    "preiskategorie_ungueltig",
                    $"Preiskategorie '{ergebnis.UngueltigePreiskategorieId}' gehört nicht zu dieser Veranstaltung."),
            BuchungErgebnisTyp.SitzplatzDuplikat =>
                ProblemMitTyp(
                    StatusCodes.Status400BadRequest,
                    "Sitzplatz mehrfach ausgewählt",
                    "sitzplatz_duplikat",
                    "Mindestens ein Sitzplatz wurde mehrfach in derselben Buchung angefragt."),
            BuchungErgebnisTyp.SitzplatzKonflikt =>
                ProblemMitTyp(
                    StatusCodes.Status409Conflict,
                    "Sitzplatz belegt",
                    "sitzplatz_belegt",
                    "Mindestens einer der gewählten Sitzplätze wurde inzwischen belegt.",
                    ergebnis.BetroffeneSitzplaetze),
            BuchungErgebnisTyp.ReferenzErzeugungFehlgeschlagen =>
                ProblemMitTyp(
                    StatusCodes.Status500InternalServerError,
                    "Buchungsreferenz konnte nicht erzeugt werden",
                    "buchungsreferenz_erzeugung_fehlgeschlagen"),
            _ => throw new InvalidOperationException($"Unbekannter BuchungErgebnisTyp: {ergebnis.Typ}"),
        };
    }

    private ObjectResult ProblemMitTyp(
        int status,
        string titel,
        string typ,
        string? detail = null,
        IReadOnlyList<string>? betroffeneSitzplaetze = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = titel,
            Type = typ,
            Detail = detail,
        };

        if (betroffeneSitzplaetze is not null)
        {
            problemDetails.Extensions["betroffeneSitzplaetze"] = betroffeneSitzplaetze;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
