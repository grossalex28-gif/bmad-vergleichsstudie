using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/spielstaetten")]
public class SpielstaettenController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpielstaetteDto>>> GetAlle(CancellationToken ct)
    {
        var spielstaetten = await db.Spielstaetten
            .OrderBy(s => s.Name)
            .Select(s => new SpielstaetteDto(s.Id, s.Name))
            .ToListAsync(ct);

        return Ok(spielstaetten);
    }
}
