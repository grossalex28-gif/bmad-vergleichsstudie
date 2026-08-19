using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.DTOs;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Services;

public class BuchungenService(AppDbContext db, IReferenzGenerator referenzGenerator) : IBuchungenService
{
    private const int MaxReferenzVersuche = 5;

    public async Task<BuchungErgebnis> BuchungAnlegenAsync(string veranstaltungId, BuchungAnlegenRequestDto request)
    {
        var veranstaltung = await db.Veranstaltungen
            .Include(v => v.Preiskategorien)
            .FirstOrDefaultAsync(v => v.Id == veranstaltungId);

        if (veranstaltung is null)
        {
            return BuchungErgebnis.VeranstaltungFehlt();
        }

        foreach (var position in request.Positionen)
        {
            if (veranstaltung.Preiskategorien.All(p => p.Id != position.PreiskategorieId))
            {
                return BuchungErgebnis.PreiskategorieFehlt(position.PreiskategorieId);
            }
        }

        var angefragteCodes = request.Positionen.Select(p => p.SitzplatzCode).ToList();

        if (angefragteCodes.Count != angefragteCodes.Distinct().Count())
        {
            return BuchungErgebnis.SitzplatzDuplikat();
        }

        for (var versuch = 1; versuch <= MaxReferenzVersuche; versuch++)
        {
            var buchung = new Buchung
            {
                Id = Guid.NewGuid().ToString(),
                Referenz = referenzGenerator.Naechste(),
                VeranstaltungId = veranstaltungId,
                Name = request.Name,
                Email = request.Email,
                Status = BuchungStatus.Aktiv,
                Gesamtpreis = 0m,
            };

            var positionen = new List<Buchungsposition>();
            var gesamtpreis = 0m;
            foreach (var position in request.Positionen)
            {
                var kategorie = veranstaltung.Preiskategorien.First(p => p.Id == position.PreiskategorieId);
                gesamtpreis += kategorie.Preis;
                positionen.Add(new Buchungsposition
                {
                    BuchungId = buchung.Id,
                    SitzplatzCode = position.SitzplatzCode,
                    PreiskategorieId = kategorie.Id,
                    PreisSnapshot = kategorie.Preis,
                });
            }

            buchung.Gesamtpreis = gesamtpreis;

            var belegungen = request.Positionen.Select(position => new Sitzplatzbelegung
            {
                VeranstaltungId = veranstaltungId,
                SitzplatzCode = position.SitzplatzCode,
                BuchungId = buchung.Id,
            }).ToList();

            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Buchungen.Add(buchung);
            db.Buchungspositionen.AddRange(positionen);
            db.Sitzplatzbelegungen.AddRange(belegungen);

            try
            {
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return BuchungErgebnis.Erfolg(new BuchungDto(
                    buchung.Referenz,
                    buchung.VeranstaltungId,
                    buchung.Name,
                    buchung.Email,
                    buchung.Status.ToString(),
                    buchung.Gesamtpreis,
                    positionen.Select(p => new BuchungspositionDto(
                        p.SitzplatzCode,
                        p.PreiskategorieId,
                        veranstaltung.Preiskategorien.First(k => k.Id == p.PreiskategorieId).Name,
                        p.PreisSnapshot)).ToList()));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                db.ChangeTracker.Clear();

                var tatsaechlichBelegt = await db.Sitzplatzbelegungen
                    .Where(s => s.VeranstaltungId == veranstaltungId && angefragteCodes.Contains(s.SitzplatzCode))
                    .Select(s => s.SitzplatzCode)
                    .ToListAsync();

                if (tatsaechlichBelegt.Count > 0)
                {
                    return BuchungErgebnis.Konflikt(tatsaechlichBelegt);
                }

                if (ex.InnerException is not SqlException { Number: 2601 or 2627 })
                {
                    // Keiner der angefragten Plaetze ist belegt, und der Fehler ist keine
                    // UNIQUE-Constraint-Verletzung (SQL-Fehlercodes 2601/2627 fuer Index bzw.
                    // Primaerschluessel) - z.B. ein Deadlock oder Verbindungsabbruch. Das ist keine
                    // Referenz-Kollision und darf nicht als solche wegretryt werden.
                    throw;
                }

                // Keiner der angefragten Plaetze ist tatsaechlich belegt und der Fehler ist eine
                // UNIQUE-Constraint-Verletzung: der Fehlschlag kam von der UNIQUE-Constraint auf
                // Buchung.Referenz. Naechster Versuch mit neuer Referenz (bis MaxReferenzVersuche
                // erreicht ist).
            }
        }

        return BuchungErgebnis.ReferenzFehlgeschlagen();
    }

    public async Task<BuchungDetailDto?> BuchungAbrufenAsync(string referenz)
    {
        var referenzNormalisiert = referenz.Trim().ToUpperInvariant();

        return await db.Buchungen
            .Where(b => b.Referenz == referenzNormalisiert)
            .Select(b => new BuchungDetailDto(
                b.Referenz,
                b.VeranstaltungId,
                b.Veranstaltung!.Titel,
                b.Veranstaltung!.Zeitpunkt,
                b.Veranstaltung!.Raum!.Spielstaette!.Name,
                b.Veranstaltung!.Raum!.Name,
                b.Name,
                b.Email,
                b.Status.ToString(),
                b.Gesamtpreis,
                b.Buchungspositionen
                    .OrderBy(p => p.SitzplatzCode)
                    .Select(p => new BuchungspositionDto(p.SitzplatzCode, p.PreiskategorieId, p.Preiskategorie!.Name, p.PreisSnapshot))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<StornierungErgebnis> BuchungStornierenAsync(string referenz)
    {
        var referenzNormalisiert = referenz.Trim().ToUpperInvariant();

        await using var transaction = await db.Database.BeginTransactionAsync();

        var aktualisierteZeilen = await db.Buchungen
            .Where(b => b.Referenz == referenzNormalisiert && b.Status == BuchungStatus.Aktiv)
            .ExecuteUpdateAsync(setter => setter.SetProperty(b => b.Status, BuchungStatus.Storniert));

        if (aktualisierteZeilen == 0)
        {
            await transaction.RollbackAsync();
            var existiert = await db.Buchungen.AnyAsync(b => b.Referenz == referenzNormalisiert);
            return existiert ? StornierungErgebnis.BereitsStorniert() : StornierungErgebnis.NichtGefunden();
        }

        var buchungId = await db.Buchungen
            .Where(b => b.Referenz == referenzNormalisiert)
            .Select(b => b.Id)
            .FirstAsync();

        await db.Sitzplatzbelegungen
            .Where(s => s.BuchungId == buchungId)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();

        var aktualisiert = await BuchungAbrufenAsync(referenzNormalisiert);
        return StornierungErgebnis.Erfolg(aktualisiert!);
    }
}
