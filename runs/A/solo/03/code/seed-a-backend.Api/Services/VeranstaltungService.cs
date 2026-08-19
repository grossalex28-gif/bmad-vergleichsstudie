using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;

namespace seed_a_backend.Api.Services;

public class VeranstaltungService(AppDbContext context) : IVeranstaltungService
{
    public async Task<IReadOnlyList<SpielstaetteListItemDto>> GetSpielstaettenAsync(CancellationToken cancellationToken = default)
    {
        return await context.Spielstaetten
            .OrderBy(s => s.Name)
            .Select(s => new SpielstaetteListItemDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VeranstaltungListItemDto>> GetVeranstaltungenAsync(
        DateTime? von,
        DateTime? bis,
        string? spielstaetteId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Veranstaltungen.Include(v => v.Spielstaette).AsQueryable();

        if (von.HasValue)
        {
            query = query.Where(v => v.Zeitpunkt >= von.Value);
        }

        if (bis.HasValue)
        {
            query = query.Where(v => v.Zeitpunkt <= bis.Value);
        }

        if (!string.IsNullOrWhiteSpace(spielstaetteId))
        {
            query = query.Where(v => v.SpielstaetteId == spielstaetteId);
        }

        return await query
            .OrderBy(v => v.Zeitpunkt)
            .Select(v => new VeranstaltungListItemDto(v.Id, v.Titel, v.SpielstaetteId, v.Spielstaette!.Name, v.Zeitpunkt))
            .ToListAsync(cancellationToken);
    }

    public async Task<VeranstaltungDetailDto> GetDetailAsync(string veranstaltungId, CancellationToken cancellationToken = default)
    {
        var veranstaltung = await context.Veranstaltungen
            .Include(v => v.Spielstaette)
            .Include(v => v.Raum)
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == veranstaltungId, cancellationToken)
            ?? throw new NotFoundException($"Veranstaltung '{veranstaltungId}' wurde nicht gefunden.");

        return new VeranstaltungDetailDto(
            veranstaltung.Id,
            veranstaltung.Titel,
            veranstaltung.Beschreibung,
            veranstaltung.Zeitpunkt,
            veranstaltung.DauerMinuten,
            veranstaltung.Altersfreigabe,
            new SpielstaetteListItemDto(veranstaltung.Spielstaette!.Id, veranstaltung.Spielstaette.Name),
            new RaumInfoDto(veranstaltung.Raum!.Id, veranstaltung.Raum.Name),
            veranstaltung.Preiskategorien
                .OrderBy(p => p.Id)
                .Select(p => new PreiskategorieDto(p.Id, p.Name, p.Preis))
                .ToList());
    }

    public async Task<SitzplanDto> GetSitzplanAsync(string veranstaltungId, CancellationToken cancellationToken = default)
    {
        var veranstaltung = await context.Veranstaltungen
            .Include(v => v.Raum)
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == veranstaltungId, cancellationToken)
            ?? throw new NotFoundException($"Veranstaltung '{veranstaltungId}' wurde nicht gefunden.");

        var raum = veranstaltung.Raum!;

        var belegteSitzplaetze = await context.BuchungsPositionen
            .Where(p => p.VeranstaltungId == veranstaltungId && p.Aktiv)
            .Select(p => new { p.Reihe, p.Spalte })
            .ToListAsync(cancellationToken);

        var belegtSet = belegteSitzplaetze.Select(p => (p.Reihe, p.Spalte)).ToHashSet();
        var gangSet = raum.GangSpalten.ToHashSet();

        var sitzplaetze = new List<SitzplatzDto>();
        foreach (var reihe in raum.Reihen)
        {
            for (var spalte = 1; spalte <= raum.Spalten; spalte++)
            {
                if (gangSet.Contains(spalte))
                {
                    sitzplaetze.Add(new SitzplatzDto(reihe, spalte, SitzplatzTyp.Gang, null));
                }
                else
                {
                    var status = belegtSet.Contains((reihe, spalte)) ? SitzplatzStatus.Belegt : SitzplatzStatus.Frei;
                    sitzplaetze.Add(new SitzplatzDto(reihe, spalte, SitzplatzTyp.Sitzplatz, status));
                }
            }
        }

        return new SitzplanDto(
            veranstaltung.Id,
            new RaumSitzplanDto(raum.Reihen, raum.Spalten, raum.GangSpalten, raum.GangHinweis),
            sitzplaetze,
            veranstaltung.Preiskategorien
                .OrderBy(p => p.Id)
                .Select(p => new PreiskategorieDto(p.Id, p.Name, p.Preis))
                .ToList());
    }
}
