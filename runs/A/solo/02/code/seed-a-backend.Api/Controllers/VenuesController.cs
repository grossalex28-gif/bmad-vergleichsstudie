using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VenueDto>>> GetAll(CancellationToken ct)
    {
        var venues = await db.Venues
            .Include(v => v.Rooms)
            .OrderBy(v => v.Name)
            .Select(v => new VenueDto(
                v.Id,
                v.Name,
                v.Rooms.OrderBy(r => r.Name).Select(r => new RoomSummaryDto(r.Id, r.Name)).ToList()))
            .ToListAsync(ct);

        return Ok(venues);
    }
}
