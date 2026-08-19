using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Services;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EventListItemDto>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? venueId,
        CancellationToken ct)
    {
        var query = db.Events.Include(e => e.Venue).AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(e => e.StartsAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.StartsAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(venueId))
        {
            query = query.Where(e => e.VenueId == venueId);
        }

        var events = await query
            .OrderBy(e => e.StartsAt)
            .Select(e => new EventListItemDto(e.Id, e.Title, e.VenueId, e.Venue.Name, e.StartsAt))
            .ToListAsync(ct);

        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDetailDto>> GetById(string id, CancellationToken ct)
    {
        var @event = await db.Events
            .Include(e => e.Venue)
            .Include(e => e.Room)
            .Include(e => e.PriceCategories)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (@event is null)
        {
            return NotFound();
        }

        var dto = new EventDetailDto(
            @event.Id,
            @event.Title,
            @event.Description,
            @event.DurationMinutes,
            @event.AgeRating,
            @event.VenueId,
            @event.Venue.Name,
            @event.RoomId,
            @event.Room.Name,
            @event.StartsAt,
            @event.PriceCategories
                .OrderByDescending(c => c.Price)
                .Select(c => new PriceCategoryDto(c.Id, c.Name, c.Price))
                .ToList());

        return Ok(dto);
    }

    [HttpGet("{id}/seatmap")]
    public async Task<ActionResult<SeatMapDto>> GetSeatMap(string id, CancellationToken ct)
    {
        var @event = await db.Events
            .Include(e => e.Room)
            .Include(e => e.BookingSeats)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (@event is null)
        {
            return NotFound();
        }

        return Ok(SeatMapBuilder.Build(@event, @event.Room, @event.BookingSeats));
    }
}
