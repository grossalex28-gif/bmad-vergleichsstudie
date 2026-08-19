using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Api.Application;

/// <summary>
/// Preisberechnung ausschließlich serverseitig aus PriceCategory (AD-3, NFR-6) — die Command-Form
/// enthält bewusst kein Preisfeld, ein evtl. clientseitig berechneter Preis kann also strukturell
/// nicht mitgesendet werden. Konflikterkennung über den Constraint-Namen aus der SqlException
/// (AD-1): nur eine Verletzung des Sitzplatz-Unique-Index führt zu SEAT_CONFLICT, eine Reference-
/// Kollision löst eine interne Neuerzeugung mit Retry aus, die der Besucher nie sieht.
/// </summary>
public class BookingService(AppDbContext db)
{
    private const string ReferenceAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ"; // ohne 0/O/1/I/L (AD-4)
    private const int ReferenceLength = 8;
    private const int MaxReferenceAttempts = 5;
    private const int MaxNameLength = 200;
    private const int MaxEmailLength = 254; // RFC 5321

    public async Task<BookingCreateResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken ct = default)
    {
        var room = await (
            from e in db.Events
            join r in db.Rooms on e.RoomId equals r.Id
            where e.Id == command.EventId
            select r
        ).SingleOrDefaultAsync(ct);

        if (room is null)
        {
            return new BookingCreateResult.EventNotFound();
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new BookingCreateResult.ValidationFailed("Der Name darf nicht leer sein.");
        }

        if (command.Name.Length > MaxNameLength)
        {
            return new BookingCreateResult.ValidationFailed($"Der Name darf höchstens {MaxNameLength} Zeichen lang sein.");
        }

        if (command.Email.Length > MaxEmailLength)
        {
            return new BookingCreateResult.ValidationFailed($"Die E-Mail-Adresse darf höchstens {MaxEmailLength} Zeichen lang sein.");
        }

        if (!MailAddress.TryCreate(command.Email, out var parsedEmail))
        {
            return new BookingCreateResult.ValidationFailed("Die E-Mail-Adresse ist ungültig.");
        }

        if (command.Positions.Count == 0)
        {
            return new BookingCreateResult.ValidationFailed("Es muss mindestens ein Sitzplatz ausgewählt sein.");
        }

        var maxSeatsInRoom = room.RowLabels.Count * room.ColumnCount;
        if (command.Positions.Count > maxSeatsInRoom)
        {
            return new BookingCreateResult.ValidationFailed("Es können nicht mehr Sitzplätze gebucht werden, als der Raum enthält.");
        }

        if (command.Positions.GroupBy(p => (p.RowLabel, p.ColumnNumber)).Any(g => g.Count() > 1))
        {
            return new BookingCreateResult.ValidationFailed("Ein Sitzplatz ist mehrfach in der Auswahl enthalten.");
        }

        var priceCategories = await db.PriceCategories
            .Where(pc => pc.EventId == command.EventId)
            .ToDictionaryAsync(pc => pc.Id, ct);

        foreach (var position in command.Positions)
        {
            var seatCode = SeatCode(position.RowLabel, position.ColumnNumber);

            if (!room.RowLabels.Contains(position.RowLabel))
            {
                return new BookingCreateResult.ValidationFailed($"Sitzplatz '{seatCode}' existiert nicht in diesem Raum.");
            }
            if (position.ColumnNumber < 1 || position.ColumnNumber > room.ColumnCount)
            {
                return new BookingCreateResult.ValidationFailed($"Sitzplatz '{seatCode}' liegt außerhalb des Sitzplans.");
            }
            if (room.AisleColumns.Contains(position.ColumnNumber))
            {
                return new BookingCreateResult.ValidationFailed($"'{seatCode}' ist ein Gang, kein Sitzplatz.");
            }
            if (!priceCategories.ContainsKey(position.PriceCategoryId))
            {
                return new BookingCreateResult.ValidationFailed("Die gewählte Preiskategorie gehört nicht zu dieser Veranstaltung.");
            }
        }

        // Deterministische Reihenfolge (RowLabel, dann ColumnNumber) zur Deadlock-Vermeidung bei
        // gegenläufigen gleichzeitigen Insert-Reihenfolgen (AD-1).
        var orderedPositions = command.Positions
            .OrderBy(p => p.RowLabel, StringComparer.Ordinal)
            .ThenBy(p => p.ColumnNumber)
            .ToList();

        var booking = new Booking
        {
            Reference = GenerateReference(),
            Name = command.Name.Trim(),
            Email = parsedEmail!.Address,
        };
        foreach (var position in orderedPositions)
        {
            booking.Positions.Add(new BookingPosition
            {
                EventId = command.EventId,
                RowLabel = position.RowLabel,
                ColumnNumber = position.ColumnNumber,
                PriceCategoryId = position.PriceCategoryId,
            });
        }

        db.Bookings.Add(booking);

        for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);

                var positionResults = orderedPositions
                    .Select(p => new BookingPositionResult(p.RowLabel, p.ColumnNumber, ToSummary(priceCategories[p.PriceCategoryId])))
                    .ToList();
                var totalPrice = orderedPositions.Sum(p => priceCategories[p.PriceCategoryId].Preis);

                return new BookingCreateResult.Success(new BookingResult(
                    booking.Reference, booking.Name,
                    booking.CancelledAtUtc is null ? "aktiv" : "storniert",
                    positionResults, totalPrice));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
            {
                if (sql.Message.Contains(AppDbContext.BookingPositionSeatUniqueConstraintName))
                {
                    // Re-Read dient nur der Nutzer-Kommunikation, nicht der Korrektheitsprüfung
                    // (die ist bereits durch den fehlgeschlagenen Insert entschieden, AD-1).
                    var occupied = await db.BookingPositions
                        .Where(bp => bp.EventId == command.EventId && bp.CancelledAtUtc == null)
                        .Select(bp => new { bp.RowLabel, bp.ColumnNumber })
                        .ToListAsync(ct);
                    var occupiedSet = occupied.Select(o => (o.RowLabel, o.ColumnNumber)).ToHashSet();
                    var conflicting = orderedPositions
                        .Where(p => occupiedSet.Contains((p.RowLabel, p.ColumnNumber)))
                        .Select(p => SeatCode(p.RowLabel, p.ColumnNumber))
                        .ToList();
                    if (conflicting.Count == 0)
                    {
                        // Die konkurrierende Buchung wurde zwischen dem fehlgeschlagenen Insert und
                        // diesem Re-Read bereits storniert — der Konflikt ist real (Insert ist gescheitert),
                        // aber keine der angefragten Positionen ist mehr belegt. Sicherer Fallback: alle
                        // angefragten Sitzplätze melden, statt eine leere Liste zurückzugeben.
                        conflicting = orderedPositions
                            .Select(p => SeatCode(p.RowLabel, p.ColumnNumber))
                            .ToList();
                    }
                    return new BookingCreateResult.SeatConflict(conflicting);
                }

                if (sql.Message.Contains(AppDbContext.BookingReferenceUniqueConstraintName))
                {
                    booking.Reference = GenerateReference();
                    continue;
                }

                throw;
            }
        }

        throw new InvalidOperationException("Es konnte keine eindeutige Buchungsreferenz erzeugt werden.");
    }

    /// <summary>Liest eine Buchung per Referenz (case-insensitiver Vergleich über die Spaltenkollation, AD-4).</summary>
    public async Task<BookingResult?> GetBookingByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var booking = await FindBookingWithPositionsAsync(reference, ct);

        return booking is null ? null : await ToBookingResultAsync(booking, ct);
    }

    /// <summary>
    /// Storniert eine aktive Buchung (Soft-Cancel, AD-2): setzt <c>CancelledAtUtc</c> auf die Buchung und
    /// jede zugehörige Position im selben <see cref="AppDbContext.SaveChangesAsync(CancellationToken)"/>-Aufruf,
    /// damit kein Teilzustand entstehen kann. Der gefilterte Unique-Index (AD-1) gibt die Sitzplätze danach
    /// automatisch wieder frei.
    /// </summary>
    public async Task<BookingCancelResult> CancelBookingAsync(string reference, CancellationToken ct = default)
    {
        var booking = await FindBookingWithPositionsAsync(reference, ct);

        if (booking is null)
        {
            return new BookingCancelResult.NotFound();
        }

        if (booking.CancelledAtUtc is not null)
        {
            return new BookingCancelResult.AlreadyCancelled();
        }

        var now = DateTime.UtcNow;
        booking.CancelledAtUtc = now;
        foreach (var position in booking.Positions)
        {
            position.CancelledAtUtc = now;
        }

        await db.SaveChangesAsync(ct);

        return new BookingCancelResult.Success(await ToBookingResultAsync(booking, ct));
    }

    private Task<Booking?> FindBookingWithPositionsAsync(string reference, CancellationToken ct) =>
        db.Bookings
            .Include(b => b.Positions)
            .SingleOrDefaultAsync(b => b.Reference == reference, ct);

    private async Task<BookingResult> ToBookingResultAsync(Booking booking, CancellationToken ct)
    {
        var priceCategoryIds = booking.Positions.Select(p => p.PriceCategoryId!.Value).ToHashSet();
        var priceCategories = await db.PriceCategories
            .Where(pc => priceCategoryIds.Contains(pc.Id))
            .ToDictionaryAsync(pc => pc.Id, ct);

        var orderedPositions = booking.Positions
            .OrderBy(p => p.RowLabel, StringComparer.Ordinal)
            .ThenBy(p => p.ColumnNumber)
            .ToList();

        var positionResults = orderedPositions
            .Select(p => new BookingPositionResult(p.RowLabel, p.ColumnNumber, ToSummary(priceCategories[p.PriceCategoryId!.Value])))
            .ToList();
        var totalPrice = orderedPositions.Sum(p => priceCategories[p.PriceCategoryId!.Value].Preis);

        return new BookingResult(
            booking.Reference, booking.Name,
            booking.CancelledAtUtc is null ? "aktiv" : "storniert",
            positionResults, totalPrice);
    }

    private static PriceCategorySummary ToSummary(PriceCategory pc) => new(pc.Id, pc.Name, pc.Preis);

    /// <summary>Kombinierte Wire-/Anzeige-Form eines Sitzplatzes, z. B. "B3" (Consistency Convention).</summary>
    private static string SeatCode(string rowLabel, int columnNumber) => $"{rowLabel}{columnNumber}";

    private static string GenerateReference()
    {
        Span<char> chars = stackalloc char[ReferenceLength];
        for (var i = 0; i < ReferenceLength; i++)
        {
            chars[i] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];
        }
        return new string(chars);
    }
}
