using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Controllers.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController(VenueQueryService venueQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetVenues(CancellationToken ct)
    {
        var venues = await venueQueryService.GetVenuesAsync(ct);
        return Ok(venues.Select(v => new VenueDto(v.Id, v.Name)));
    }
}
