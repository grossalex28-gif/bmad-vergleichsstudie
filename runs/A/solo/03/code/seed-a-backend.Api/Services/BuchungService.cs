using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Services;

public partial class BuchungService(AppDbContext context) : IBuchungService
{
    private const string ReferenzZeichen = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // ohne verwechselbare Zeichen (I,O,0,1)

    public async Task<BuchungDto> CreateBuchungAsync(CreateBuchungRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateStammdaten(request);

        var veranstaltung = await context.Veranstaltungen
            .Include(v => v.Raum)
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == request.VeranstaltungId, cancellationToken)
            ?? throw new NotFoundException($"Veranstaltung '{request.VeranstaltungId}' wurde nicht gefunden.");

        var raum = veranstaltung.Raum!;
        var gueltigeReihen = raum.Reihen.ToHashSet();
        var gangSet = raum.GangSpalten.ToHashSet();
        var preiskategorien = veranstaltung.Preiskategorien.ToDictionary(p => p.Id);

        var angefragteSitzplaetze = new HashSet<(string Reihe, int Spalte)>();
        foreach (var sitzplatz in request.Sitzplaetze)
        {
            if (!gueltigeReihen.Contains(sitzplatz.Reihe) || sitzplatz.Spalte < 1 || sitzplatz.Spalte > raum.Spalten)
            {
                throw new ValidationException($"Sitzplatz '{sitzplatz.Reihe}{sitzplatz.Spalte}' existiert nicht in diesem Raum.");
            }

            if (gangSet.Contains(sitzplatz.Spalte))
            {
                throw new ValidationException($"'{sitzplatz.Reihe}{sitzplatz.Spalte}' ist ein Gang und kein Sitzplatz.");
            }

            if (!preiskategorien.ContainsKey(sitzplatz.PreiskategorieId))
            {
                throw new ValidationException($"Preiskategorie '{sitzplatz.PreiskategorieId}' gehört nicht zu dieser Veranstaltung.");
            }

            if (!angefragteSitzplaetze.Add((sitzplatz.Reihe, sitzplatz.Spalte)))
            {
                throw new ValidationException($"Sitzplatz '{sitzplatz.Reihe}{sitzplatz.Spalte}' wurde mehrfach ausgewählt.");
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var bereitsBelegt = await context.BuchungsPositionen
                .Where(p => p.VeranstaltungId == veranstaltung.Id && p.Aktiv)
                .Select(p => new { p.Reihe, p.Spalte })
                .ToListAsync(cancellationToken);

            var belegtSet = bereitsBelegt.Select(p => (p.Reihe, p.Spalte)).ToHashSet();
            var konflikte = angefragteSitzplaetze.Where(belegtSet.Contains).ToList();
            if (konflikte.Count > 0)
            {
                throw new SeatConflictException(konflikte);
            }

            var buchung = new Buchung
            {
                Referenz = await NeueEindeutigeReferenzAsync(cancellationToken),
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                VeranstaltungId = veranstaltung.Id,
                Status = BuchungStatus.Bestaetigt,
                ErstelltAm = DateTime.UtcNow
            };

            foreach (var sitzplatz in request.Sitzplaetze)
            {
                var preiskategorie = preiskategorien[sitzplatz.PreiskategorieId];
                buchung.Positionen.Add(new BuchungsPosition
                {
                    VeranstaltungId = veranstaltung.Id,
                    Reihe = sitzplatz.Reihe,
                    Spalte = sitzplatz.Spalte,
                    PreiskategorieId = preiskategorie.Id,
                    Preis = preiskategorie.Preis,
                    Aktiv = true
                });
            }

            context.Buchungen.Add(buchung);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Ein anderer Vorgang hat zwischen Prüfung und Speichern denselben Platz belegt.
                throw new SeatConflictException(angefragteSitzplaetze.ToList());
            }

            await transaction.CommitAsync(cancellationToken);

            return ToDto(buchung, veranstaltung, preiskategorien);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BuchungDto> GetByReferenzAsync(string referenz, CancellationToken cancellationToken = default)
    {
        var buchung = await LoadBuchungAsync(referenz, cancellationToken);
        return ToDto(buchung, buchung.Veranstaltung!, buchung.Veranstaltung!.Preiskategorien.ToDictionary(p => p.Id));
    }

    public async Task<BuchungDto> StornierenAsync(string referenz, CancellationToken cancellationToken = default)
    {
        var buchung = await LoadBuchungAsync(referenz, cancellationToken);

        if (buchung.Status == BuchungStatus.Storniert)
        {
            throw new ValidationException("Diese Buchung wurde bereits storniert.");
        }

        buchung.Status = BuchungStatus.Storniert;
        foreach (var position in buchung.Positionen)
        {
            position.Aktiv = false;
        }

        await context.SaveChangesAsync(cancellationToken);

        return ToDto(buchung, buchung.Veranstaltung!, buchung.Veranstaltung!.Preiskategorien.ToDictionary(p => p.Id));
    }

    private async Task<Buchung> LoadBuchungAsync(string referenz, CancellationToken cancellationToken)
    {
        return await context.Buchungen
            .Include(b => b.Positionen)
            .Include(b => b.Veranstaltung!).ThenInclude(v => v.Preiskategorien)
            .FirstOrDefaultAsync(b => b.Referenz == referenz, cancellationToken)
            ?? throw new NotFoundException($"Buchung mit Referenz '{referenz}' wurde nicht gefunden.");
    }

    private static void ValidateStammdaten(CreateBuchungRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.VeranstaltungId))
        {
            throw new ValidationException("Veranstaltung ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Name ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailPattern().IsMatch(request.Email))
        {
            throw new ValidationException("Eine gültige E-Mail-Adresse ist erforderlich.");
        }

        if (request.Sitzplaetze is null || request.Sitzplaetze.Count == 0)
        {
            throw new ValidationException("Es muss mindestens ein Sitzplatz ausgewählt werden.");
        }
    }

    private async Task<string> NeueEindeutigeReferenzAsync(CancellationToken cancellationToken)
    {
        for (var versuch = 0; versuch < 10; versuch++)
        {
            var kandidat = GeneriereReferenz();
            var existiert = await context.Buchungen.AnyAsync(b => b.Referenz == kandidat, cancellationToken);
            if (!existiert)
            {
                return kandidat;
            }
        }

        throw new InvalidOperationException("Es konnte keine eindeutige Buchungsreferenz erzeugt werden.");
    }

    private static string GeneriereReferenz()
    {
        Span<char> buffer = stackalloc char[8];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = ReferenzZeichen[Random.Shared.Next(ReferenzZeichen.Length)];
        }

        return new string(buffer);
    }

    private static BuchungDto ToDto(Buchung buchung, Veranstaltung veranstaltung, IReadOnlyDictionary<string, Preiskategorie> preiskategorien)
    {
        var positionen = buchung.Positionen
            .OrderBy(p => p.Reihe).ThenBy(p => p.Spalte)
            .Select(p => new BuchungspositionDto(
                p.Reihe,
                p.Spalte,
                p.PreiskategorieId,
                preiskategorien.TryGetValue(p.PreiskategorieId, out var pk) ? pk.Name : p.PreiskategorieId,
                p.Preis))
            .ToList();

        return new BuchungDto(
            buchung.Referenz,
            veranstaltung.Id,
            veranstaltung.Titel,
            veranstaltung.Zeitpunkt,
            buchung.Name,
            buchung.Email,
            buchung.ErstelltAm,
            buchung.Status,
            positionen,
            positionen.Sum(p => p.Preis));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
