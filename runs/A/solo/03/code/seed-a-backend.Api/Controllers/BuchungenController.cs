using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/buchungen")]
public class BuchungenController(IBuchungService buchungService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BuchungDto>> Create([FromBody] CreateBuchungRequestDto request, CancellationToken cancellationToken)
    {
        var buchung = await buchungService.CreateBuchungAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByReferenz), new { referenz = buchung.Referenz }, buchung);
    }

    [HttpGet("{referenz}")]
    public async Task<ActionResult<BuchungDto>> GetByReferenz(string referenz, CancellationToken cancellationToken)
    {
        return Ok(await buchungService.GetByReferenzAsync(referenz, cancellationToken));
    }

    [HttpPost("{referenz}/stornieren")]
    public async Task<ActionResult<BuchungDto>> Stornieren(string referenz, CancellationToken cancellationToken)
    {
        return Ok(await buchungService.StornierenAsync(referenz, cancellationToken));
    }
}
