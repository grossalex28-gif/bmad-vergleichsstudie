using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Api.Application;

/// <summary>
/// `Domain/Room` und `Domain/Venue` haben keine Navigationseigenschaft zurück zum Elternobjekt
/// (alle Beziehungen sind in `AppDbContext` einseitig mit `.WithOne()` ohne Inverse konfiguriert),
/// daher ein expliziter Join über die FK-Spalten statt `.Include()`. `Event.PriceCategories` ist
/// dagegen eine echte Navigationseigenschaft, dafür ist kein zusätzlicher Join nötig.
/// </summary>
public class EventDetailService(AppDbContext db)
{
    public async Task<EventDetail?> GetEventDetailAsync(int id, CancellationToken ct = default) =>
        await (
            from e in db.Events
            join r in db.Rooms on e.RoomId equals r.Id
            join v in db.Venues on r.VenueId equals v.Id
            where e.Id == id
            select new EventDetail(
                e.Id, e.Titel, e.Beschreibung, e.DauerMinuten, e.Altersfreigabe,
                v.Name, r.Name, e.Zeitpunkt,
                e.PriceCategories.OrderBy(pc => pc.Id)
                    .Select(pc => new PriceCategorySummary(pc.Id, pc.Name, pc.Preis)).ToList())
        ).SingleOrDefaultAsync(ct);
}
