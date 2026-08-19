using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application.Dtos;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Api.Application;

public class SeatMapService(AppDbContext dbContext)
{
    public async Task<SeatMapDto?> GetSeatMapAsync(Guid eventId)
    {
        var roomId = await dbContext.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.RoomId)
            .FirstOrDefaultAsync();

        if (roomId is null)
        {
            return null;
        }

        var room = await dbContext.Rooms
            .Where(r => r.Id == roomId.Value)
            .Select(r => new { r.Reihen, r.SpaltenAnzahl, r.GangSpalten })
            .FirstAsync();

        var occupiedSeatIds = await dbContext.BookingSeats
            .Where(bs => bs.EventId == eventId && bs.IsActive)
            .Select(bs => bs.SeatId)
            .ToHashSetAsync();

        var seatRows = await dbContext.Seats
            .Where(s => s.RoomId == roomId.Value)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Column)
            .Select(s => new { s.Id, s.Row, s.Column })
            .ToListAsync();

        var seats = seatRows
            .Select(s => new SeatDto(s.Id, s.Row, s.Column, occupiedSeatIds.Contains(s.Id) ? SeatStatus.Occupied : SeatStatus.Free))
            .ToList();

        var priceCategories = await dbContext.PriceCategories
            .Where(pc => pc.EventId == eventId)
            .OrderBy(pc => pc.Reihenfolge)
            .Select(pc => new PriceCategoryDto(pc.Id, pc.Name, pc.Preis))
            .ToListAsync();

        return new SeatMapDto(eventId, room.Reihen, room.SpaltenAnzahl, room.GangSpalten, seats, priceCategories);
    }
}
