using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Api.Application;

/// <summary>
/// `Domain/Room` und `Domain/Venue` haben keine Navigationseigenschaft zurück zum Elternobjekt
/// (alle Beziehungen sind in `AppDbContext` einseitig mit `.WithOne()` ohne Inverse konfiguriert),
/// daher ein expliziter Join über die FK-Spalten statt `.Include()`.
/// </summary>
public class EventQueryService(AppDbContext db)
{
    public async Task<List<EventSummary>> GetEventSummariesAsync(DateOnly? von, DateOnly? bis, int? venueId, CancellationToken ct = default)
    {
        var vonZeitpunkt = von?.ToDateTime(TimeOnly.MinValue);
        var bisZeitpunkt = bis is null
            ? (DateTime?)null
            : bis == DateOnly.MaxValue
                ? DateTime.MaxValue
                : bis.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await (
            from e in db.Events
            join r in db.Rooms on e.RoomId equals r.Id
            join v in db.Venues on r.VenueId equals v.Id
            where (vonZeitpunkt == null || e.Zeitpunkt >= vonZeitpunkt) && (bisZeitpunkt == null || e.Zeitpunkt < bisZeitpunkt) && (venueId == null || v.Id == venueId)
            orderby e.Zeitpunkt, e.Id
            select new EventSummary(e.Id, e.Titel, v.Name, e.Zeitpunkt)
        ).ToListAsync(ct);
    }
}
