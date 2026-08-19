using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Services;

public class BuchungService(AppDbContext db)
{
    private const int MaxReferenzVersuche = 5;

    public async Task<BuchungResponseDto> ErstelleBuchungAsync(BuchungCreateRequestDto request, CancellationToken ct)
    {
        var veranstaltung = await db.Veranstaltungen
            .Include(v => v.Raum)
            .Include(v => v.Spielstaette)
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == request.VeranstaltungId, ct)
            ?? throw new VeranstaltungNichtGefundenException(request.VeranstaltungId);

        if (request.Sitzplaetze.Count == 0)
        {
            throw new UngueltigeBuchungException("Es muss mindestens ein Sitzplatz ausgewählt werden.");
        }

        var doppelt = request.Sitzplaetze
            .GroupBy(s => (s.Reihe, s.Spalte))
            .FirstOrDefault(g => g.Count() > 1);
        if (doppelt is not null)
        {
            throw new UngueltigeBuchungException(
                $"Der Sitzplatz {doppelt.Key.Reihe}{doppelt.Key.Spalte} wurde mehrfach ausgewählt.");
        }

        foreach (var sitzplatz in request.Sitzplaetze)
        {
            if (!veranstaltung.Raum.IstGueltigerSitzplatz(sitzplatz.Reihe, sitzplatz.Spalte))
            {
                throw new UngueltigeBuchungException(
                    $"Der Sitzplatz {sitzplatz.Reihe}{sitzplatz.Spalte} existiert nicht oder ist ein Gang.");
            }

            if (veranstaltung.Preiskategorien.All(p => p.Id != sitzplatz.PreiskategorieId))
            {
                throw new UngueltigeBuchungException(
                    $"Die Preiskategorie '{sitzplatz.PreiskategorieId}' gehört nicht zu dieser Veranstaltung.");
            }
        }

        var buchung = new Buchung
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            ErstelltAm = DateTime.UtcNow,
            Status = BuchungStatus.Aktiv,
            VeranstaltungId = veranstaltung.Id
        };

        foreach (var sitzplatz in request.Sitzplaetze)
        {
            var preiskategorie = veranstaltung.Preiskategorien.First(p => p.Id == sitzplatz.PreiskategorieId);

            buchung.Positionen.Add(new Buchungsposition
            {
                Id = Guid.NewGuid(),
                BuchungId = buchung.Id,
                VeranstaltungId = veranstaltung.Id,
                Reihe = sitzplatz.Reihe,
                Spalte = sitzplatz.Spalte,
                PreiskategorieId = preiskategorie.Id,
                Preis = preiskategorie.Preis,
                Status = BuchungStatus.Aktiv
            });
        }

        for (var versuch = 1; versuch <= MaxReferenzVersuche; versuch++)
        {
            buchung.Referenz = ReferenzGenerator.NeueReferenz();
            db.Buchungen.Add(buchung);

            try
            {
                await db.SaveChangesAsync(ct);
                return ZuDto(buchung, veranstaltung);
            }
            catch (DbUpdateException)
            {
                db.Entry(buchung).State = EntityState.Detached;
                foreach (var position in buchung.Positionen)
                {
                    db.Entry(position).State = EntityState.Detached;
                }

                // Der Unique-Index kann entweder wegen eines zwischenzeitlich belegten
                // Sitzplatzes oder (extrem selten) wegen einer Referenz-Kollision ausgelöst
                // haben. Statt providerspezifische Fehlermeldungen zu parsen, wird der
                // tatsächliche Zustand erneut abgefragt, um zwischen beiden Fällen zu unterscheiden.
                var belegteSitze = await db.Buchungspositionen
                    .Where(p => p.VeranstaltungId == veranstaltung.Id && p.Status == BuchungStatus.Aktiv)
                    .Select(p => new { p.Reihe, p.Spalte })
                    .ToListAsync(ct);

                var gewuenschteSitze = request.Sitzplaetze.Select(s => (s.Reihe, s.Spalte)).ToHashSet();
                var gibKonflikt = belegteSitze.Any(b => gewuenschteSitze.Contains((b.Reihe, b.Spalte)));

                if (gibKonflikt)
                {
                    throw new SitzplatzKonfliktException(
                        "Mindestens einer der gewählten Sitzplätze wurde zwischenzeitlich belegt. Die Buchung wurde nicht angelegt.");
                }

                if (versuch < MaxReferenzVersuche)
                {
                    continue;
                }

                throw;
            }
        }

        throw new InvalidOperationException("Es konnte keine eindeutige Buchungsreferenz erzeugt werden.");
    }

    public async Task<BuchungResponseDto> HoleBuchungAsync(string referenz, CancellationToken ct)
    {
        var buchung = await LadeBuchungMitDetailsAsync(referenz, ct)
            ?? throw new BuchungNichtGefundenException(referenz);

        return ZuDto(buchung, buchung.Veranstaltung);
    }

    public async Task<BuchungResponseDto> StornierenAsync(string referenz, CancellationToken ct)
    {
        var buchung = await LadeBuchungMitDetailsAsync(referenz, ct)
            ?? throw new BuchungNichtGefundenException(referenz);

        if (buchung.Status == BuchungStatus.Storniert)
        {
            throw new BuchungBereitsStorniertException(referenz);
        }

        buchung.Status = BuchungStatus.Storniert;
        foreach (var position in buchung.Positionen)
        {
            position.Status = BuchungStatus.Storniert;
        }

        await db.SaveChangesAsync(ct);

        return ZuDto(buchung, buchung.Veranstaltung);
    }

    private Task<Buchung?> LadeBuchungMitDetailsAsync(string referenz, CancellationToken ct) =>
        db.Buchungen
            .Include(b => b.Veranstaltung).ThenInclude(v => v.Spielstaette)
            .Include(b => b.Veranstaltung).ThenInclude(v => v.Raum)
            .Include(b => b.Positionen).ThenInclude(p => p.Preiskategorie)
            .FirstOrDefaultAsync(b => b.Referenz == referenz, ct);

    private static BuchungResponseDto ZuDto(Buchung buchung, Veranstaltung veranstaltung) => new(
        buchung.Referenz,
        buchung.Name,
        buchung.Email,
        buchung.Status.ToString(),
        buchung.ErstelltAm,
        veranstaltung.Id,
        veranstaltung.Titel,
        veranstaltung.Zeitpunkt,
        veranstaltung.Spielstaette.Name,
        veranstaltung.Raum.Name,
        buchung.Positionen
            .OrderBy(p => p.Reihe).ThenBy(p => p.Spalte)
            .Select(p => new BuchungPositionResponseDto(
                p.Reihe, p.Spalte, p.PreiskategorieId, p.Preiskategorie.Name, p.Preis))
            .ToList(),
        buchung.Positionen.Sum(p => p.Preis)
    );
}
