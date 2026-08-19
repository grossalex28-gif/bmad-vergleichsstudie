using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/buchungen")]
public class BuchungenController(BuchungService buchungService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BuchungResponseDto>> Erstellen(
        [FromBody] BuchungCreateRequestDto request, CancellationToken ct)
    {
        try
        {
            var buchung = await buchungService.ErstelleBuchungAsync(request, ct);
            return CreatedAtAction(nameof(GetEine), new { referenz = buchung.Referenz }, buchung);
        }
        catch (VeranstaltungNichtGefundenException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UngueltigeBuchungException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (SitzplatzKonfliktException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{referenz}")]
    public async Task<ActionResult<BuchungResponseDto>> GetEine(string referenz, CancellationToken ct)
    {
        try
        {
            return Ok(await buchungService.HoleBuchungAsync(referenz, ct));
        }
        catch (BuchungNichtGefundenException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{referenz}/stornieren")]
    public async Task<ActionResult<BuchungResponseDto>> Stornieren(string referenz, CancellationToken ct)
    {
        try
        {
            return Ok(await buchungService.StornierenAsync(referenz, ct));
        }
        catch (BuchungNichtGefundenException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BuchungBereitsStorniertException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
