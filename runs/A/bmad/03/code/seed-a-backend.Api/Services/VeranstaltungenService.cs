using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.DTOs;

namespace seed_a_backend.Api.Services;

public class VeranstaltungenService(AppDbContext db) : IVeranstaltungenService
{
    public async Task<IReadOnlyList<VeranstaltungListeDto>> GetVeranstaltungenAsync(DateOnly? von = null, DateOnly? bis = null, string? spielstaetteId = null)
    {
        var query = db.Veranstaltungen
            .Include(v => v.Raum)
            .ThenInclude(r => r!.Spielstaette)
            .AsQueryable();

        if (von is not null)
        {
            var tagesbeginn = EuropaBerlinZeitzone.Tagesbeginn(von.Value);
            query = query.Where(v => v.Zeitpunkt >= tagesbeginn);
        }

        if (bis is not null)
        {
            var tagesende = EuropaBerlinZeitzone.Tagesende(bis.Value);
            query = query.Where(v => v.Zeitpunkt <= tagesende);
        }

        if (!string.IsNullOrWhiteSpace(spielstaetteId))
        {
            query = query.Where(v => v.Raum!.SpielstaetteId == spielstaetteId);
        }

        return await query
            .OrderBy(v => v.Zeitpunkt)
            .Select(v => new VeranstaltungListeDto(
                v.Id,
                v.Titel,
                v.Raum!.SpielstaetteId,
                v.Raum!.Spielstaette!.Name,
                v.Zeitpunkt))
            .ToListAsync();
    }

    public async Task<VeranstaltungDetailDto?> GetVeranstaltungAsync(string id)
    {
        return await db.Veranstaltungen
            .Include(v => v.Raum)
            .ThenInclude(r => r!.Spielstaette)
            .Where(v => v.Id == id)
            .Select(v => new VeranstaltungDetailDto(
                v.Id,
                v.Titel,
                v.Beschreibung,
                v.DauerMinuten,
                v.Altersfreigabe,
                v.Raum!.Spielstaette!.Name,
                v.Raum!.Name,
                v.Zeitpunkt,
                v.Preiskategorien.OrderBy(p => p.Id).Select(p => new PreiskategorieDto(p.Id, p.Name, p.Preis)).ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<SitzplanDto?> GetSitzplanAsync(string veranstaltungId)
    {
        var veranstaltung = await db.Veranstaltungen
            .Include(v => v.Raum)
            .FirstOrDefaultAsync(v => v.Id == veranstaltungId);

        if (veranstaltung is null)
        {
            return null;
        }

        var raum = veranstaltung.Raum!;

        var belegteCodes = (await db.Sitzplatzbelegungen
            .Where(s => s.VeranstaltungId == veranstaltungId)
            .Select(s => s.SitzplatzCode)
            .ToListAsync())
            .ToHashSet();

        var reihen = raum.Reihen.Select(reihe =>
        {
            var positionen = new List<SitzplanPositionDto>();
            for (var spalte = 1; spalte <= raum.Spalten; spalte++)
            {
                if (raum.GangSpalten.Contains(spalte))
                {
                    positionen.Add(new SitzplanPositionDto(spalte, "Gang", null, null));
                }
                else
                {
                    var code = $"{reihe}{spalte}";
                    positionen.Add(new SitzplanPositionDto(spalte, "Sitzplatz", code, belegteCodes.Contains(code) ? "Belegt" : "Frei"));
                }
            }

            return new SitzplanReiheDto(reihe, positionen);
        }).ToList();

        return new SitzplanDto(veranstaltung.Id, raum.Id, raum.Name, reihen);
    }
}
