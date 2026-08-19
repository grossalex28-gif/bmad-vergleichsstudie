using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Api.Application;

public class EventService(AppDbContext dbContext)
{
    public async Task<List<EventListItemDto>> GetEventsAsync(DateOnly? from = null, DateOnly? to = null, Guid? venueId = null)
    {
        IQueryable<Event> query = dbContext.Events;

        if (from is not null)
        {
            var fromStart = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.Zeitpunkt >= fromStart);
        }

        if (to is not null)
        {
            var toExclusive = to.Value == DateOnly.MaxValue
                ? DateTime.MaxValue
                : to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.Zeitpunkt < toExclusive);
        }

        if (venueId is not null)
        {
            query = query.Where(e => e.VenueId == venueId.Value);
        }

        return await query
            .OrderBy(e => e.Zeitpunkt)
            .Select(e => new EventListItemDto(e.Id, e.Titel, e.Venue!.Name, e.Zeitpunkt))
            .ToListAsync();
    }

    public async Task<EventDetailDto?> GetEventByIdAsync(Guid id)
    {
        return await dbContext.Events
            .Where(e => e.Id == id)
            .Select(e => new EventDetailDto(e.Id, e.Titel, e.Beschreibung, e.DauerMinuten, e.Altersfreigabe, e.Venue!.Name, e.Room!.Name, e.Zeitpunkt))
            .FirstOrDefaultAsync();
    }

    public async Task<List<VenueListItemDto>> GetVenuesAsync()
    {
        return await dbContext.Venues
            .OrderBy(v => v.Name)
            .Select(v => new VenueListItemDto(v.Id, v.Name))
            .ToListAsync();
    }
}
