using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Api.Application;

/// <summary>
/// Grid-Struktur kommt 1:1 aus Domain/Room (AD-8); Zellstatus wird aus BookingPosition
/// abgeleitet (AD-1/AD-2) — ein Sitzplatz gilt als belegt, wenn eine nicht stornierte
/// BookingPosition-Zeile (CancelledAtUtc IS NULL) mit passendem RowLabel/ColumnNumber
/// existiert, unabhängig davon, ob sie aus dem Anfangsdatenbestand oder einer Buchung stammt.
/// </summary>
public class SeatMapService(AppDbContext db)
{
    public async Task<SeatMap?> GetSeatMapAsync(int eventId, CancellationToken ct = default)
    {
        var room = await (
            from e in db.Events
            join r in db.Rooms on e.RoomId equals r.Id
            where e.Id == eventId
            select r
        ).SingleOrDefaultAsync(ct);

        if (room is null)
        {
            return null;
        }

        var occupiedRows = await db.BookingPositions
            .Where(bp => bp.EventId == eventId && bp.CancelledAtUtc == null)
            .Select(bp => new { bp.RowLabel, bp.ColumnNumber })
            .ToListAsync(ct);
        var occupied = occupiedRows.Select(o => (o.RowLabel, o.ColumnNumber)).ToHashSet();

        var rows = room.RowLabels
            .Select(rowLabel => new SeatMapRow(
                rowLabel,
                Enumerable.Range(1, room.ColumnCount)
                    .Select(column => new SeatMapCell(column, Status(room, occupied, rowLabel, column)))
                    .ToList()))
            .ToList();

        return new SeatMap(room.RowLabels, room.ColumnCount, room.AisleColumns.ToList(), rows);
    }

    private static string Status(Room room, HashSet<(string RowLabel, int ColumnNumber)> occupied, string rowLabel, int column) =>
        room.AisleColumns.Contains(column) ? "aisle"
            : occupied.Contains((rowLabel, column)) ? "occupied"
            : "free";
}
