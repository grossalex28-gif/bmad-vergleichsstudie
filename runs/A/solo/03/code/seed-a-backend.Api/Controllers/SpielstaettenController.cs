using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/spielstaetten")]
public class SpielstaettenController(IVeranstaltungService veranstaltungService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpielstaetteListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await veranstaltungService.GetSpielstaettenAsync(cancellationToken));
    }
}
