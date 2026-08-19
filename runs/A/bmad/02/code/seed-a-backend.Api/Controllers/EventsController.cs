using Microsoft.AspNetCore.Mvc;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Controllers.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(EventQueryService eventQueryService, EventDetailService eventDetailService, SeatMapService seatMapService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetEvents([FromQuery] DateOnly? von, [FromQuery] DateOnly? bis, [FromQuery] int? venueId, CancellationToken ct)
    {
        var summaries = await eventQueryService.GetEventSummariesAsync(von, bis, venueId, ct);
        return Ok(summaries.Select(s => new EventSummaryDto(s.Id, s.Titel, s.Spielstaette, s.Zeitpunkt)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id, CancellationToken ct)
    {
        var detail = await eventDetailService.GetEventDetailAsync(id, ct);
        if (detail is null)
        {
            return NotFound(new ErrorEnvelopeDto("EVENT_NOT_FOUND", "Veranstaltung nicht gefunden."));
        }

        return Ok(new EventDetailDto(
            detail.Id, detail.Titel, detail.Beschreibung, detail.DauerMinuten, detail.Altersfreigabe,
            detail.Spielstaette, detail.Raum, detail.Zeitpunkt,
            detail.Preiskategorien.Select(pc => new PriceCategoryDto(pc.Id, pc.Name, pc.Preis)).ToList()));
    }

    [HttpGet("{id:int}/seatmap")]
    public async Task<ActionResult<SeatMapDto>> GetSeatMap(int id, CancellationToken ct)
    {
        var seatMap = await seatMapService.GetSeatMapAsync(id, ct);
        if (seatMap is null)
        {
            return NotFound(new ErrorEnvelopeDto("EVENT_NOT_FOUND", "Veranstaltung nicht gefunden."));
        }

        return Ok(new SeatMapDto(
            seatMap.RowLabels, seatMap.ColumnCount, seatMap.AisleColumns,
            seatMap.Rows.Select(r => new SeatMapRowDto(
                r.RowLabel, r.Cells.Select(c => new SeatMapCellDto(c.ColumnNumber, c.Status)).ToList())).ToList()));
    }
}
