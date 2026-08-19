using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Controllers;

[ApiController]
[Route("api/veranstaltungen")]
public class VeranstaltungenController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeranstaltungListItemDto>>> GetAlle(
        [FromQuery] DateTime? von,
        [FromQuery] DateTime? bis,
        [FromQuery] string? spielstaetteId,
        CancellationToken ct)
    {
        var query = db.Veranstaltungen.Include(v => v.Spielstaette).AsQueryable();

        if (von is not null)
        {
            query = query.Where(v => v.Zeitpunkt >= von.Value.Date);
        }

        if (bis is not null)
        {
            query = query.Where(v => v.Zeitpunkt < bis.Value.Date.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(spielstaetteId))
        {
            query = query.Where(v => v.SpielstaetteId == spielstaetteId);
        }

        var veranstaltungen = await query
            .OrderBy(v => v.Zeitpunkt)
            .Select(v => new VeranstaltungListItemDto(v.Id, v.Titel, v.SpielstaetteId, v.Spielstaette.Name, v.Zeitpunkt))
            .ToListAsync(ct);

        return Ok(veranstaltungen);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VeranstaltungDetailDto>> GetEine(string id, CancellationToken ct)
    {
        var veranstaltung = await db.Veranstaltungen
            .Include(v => v.Spielstaette)
            .Include(v => v.Raum)
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (veranstaltung is null)
        {
            return NotFound(new { message = $"Es wurde keine Veranstaltung mit der Id '{id}' gefunden." });
        }

        var dto = new VeranstaltungDetailDto(
            veranstaltung.Id,
            veranstaltung.Titel,
            veranstaltung.Beschreibung,
            veranstaltung.DauerMinuten,
            veranstaltung.Altersfreigabe,
            veranstaltung.SpielstaetteId,
            veranstaltung.Spielstaette.Name,
            veranstaltung.RaumId,
            veranstaltung.Raum.Name,
            veranstaltung.Zeitpunkt,
            veranstaltung.Preiskategorien
                .OrderByDescending(p => p.Preis)
                .Select(p => new PreiskategorieDto(p.Id, p.Name, p.Preis))
                .ToList()
        );

        return Ok(dto);
    }

    [HttpGet("{id}/sitzplan")]
    public async Task<ActionResult<SitzplanDto>> GetSitzplan(string id, CancellationToken ct)
    {
        var veranstaltung = await db.Veranstaltungen
            .Include(v => v.Raum)
            .Include(v => v.Buchungspositionen.Where(p => p.Status == Models.BuchungStatus.Aktiv))
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (veranstaltung is null)
        {
            return NotFound(new { message = $"Es wurde keine Veranstaltung mit der Id '{id}' gefunden." });
        }

        var dto = new SitzplanDto(
            veranstaltung.Raum.Name,
            veranstaltung.Raum.Reihen,
            veranstaltung.Raum.Spalten,
            veranstaltung.Raum.GangSpalten,
            veranstaltung.Raum.GangHinweis,
            veranstaltung.Buchungspositionen
                .Select(p => new SitzplatzPositionDto(p.Reihe, p.Spalte))
                .ToList()
        );

        return Ok(dto);
    }
}
