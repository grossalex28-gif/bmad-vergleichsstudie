using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Api.Application;

public class VenueQueryService(AppDbContext db)
{
    public async Task<List<VenueSummary>> GetVenuesAsync(CancellationToken ct = default) =>
        await db.Venues
            .OrderBy(v => v.Name).ThenBy(v => v.Id)
            .Select(v => new VenueSummary(v.Id, v.Name))
            .ToListAsync(ct);
}
