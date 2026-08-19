using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/veranstaltungen")]
public class VeranstaltungenController(IVeranstaltungService veranstaltungService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeranstaltungListItemDto>>> GetAll(
        [FromQuery] DateTime? von,
        [FromQuery] DateTime? bis,
        [FromQuery] string? spielstaetteId,
        CancellationToken cancellationToken)
    {
        return Ok(await veranstaltungService.GetVeranstaltungenAsync(von, bis, spielstaetteId, cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VeranstaltungDetailDto>> GetDetail(string id, CancellationToken cancellationToken)
    {
        return Ok(await veranstaltungService.GetDetailAsync(id, cancellationToken));
    }

    [HttpGet("{id}/sitzplan")]
    public async Task<ActionResult<SitzplanDto>> GetSitzplan(string id, CancellationToken cancellationToken)
    {
        return Ok(await veranstaltungService.GetSitzplanAsync(id, cancellationToken));
    }
}
