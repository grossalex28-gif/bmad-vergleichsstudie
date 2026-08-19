using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Application.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(EventService eventService, SeatMapService seatMapService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EventListItemDto>>> GetEvents([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? venueId)
    {
        var events = await eventService.GetEventsAsync(from, to, venueId);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid id)
    {
        var eventDetail = await eventService.GetEventByIdAsync(id);
        return eventDetail is null ? NotFound() : Ok(eventDetail);
    }

    [HttpGet("/api/venues")]
    public async Task<ActionResult<List<VenueListItemDto>>> GetVenues()
    {
        var venues = await eventService.GetVenuesAsync();
        return Ok(venues);
    }

    [HttpGet("{id:guid}/sitzplan")]
    public async Task<ActionResult<SeatMapDto>> GetSeatMap(Guid id)
    {
        var seatMap = await seatMapService.GetSeatMapAsync(id);
        return seatMap is null ? NotFound() : Ok(seatMap);
    }
}
