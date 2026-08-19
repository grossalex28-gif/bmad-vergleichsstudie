using System.Data;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Dtos;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Services;

public enum CreateBookingResultType
{
    Success,
    EventNotFound,
    InvalidSeat,
    Conflict
}

public class CreateBookingResult
{
    public required CreateBookingResultType Type { get; init; }
    public Booking? Booking { get; init; }
    public string? ErrorMessage { get; init; }
    public List<(string Row, int Column)> ConflictingSeats { get; init; } = new();

    public static CreateBookingResult Success(Booking booking) =>
        new() { Type = CreateBookingResultType.Success, Booking = booking };

    public static CreateBookingResult EventNotFound() =>
        new() { Type = CreateBookingResultType.EventNotFound, ErrorMessage = "Veranstaltung wurde nicht gefunden." };

    public static CreateBookingResult InvalidSeat(string message) =>
        new() { Type = CreateBookingResultType.InvalidSeat, ErrorMessage = message };

    public static CreateBookingResult Conflict(List<(string Row, int Column)> seats) =>
        new()
        {
            Type = CreateBookingResultType.Conflict,
            ErrorMessage = "Mindestens ein gewählter Sitzplatz wurde zwischenzeitlich belegt.",
            ConflictingSeats = seats
        };
}

public class BookingService(AppDbContext db)
{
    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequestDto request, CancellationToken ct = default)
    {
        var @event = await db.Events
            .Include(e => e.Room)
            .Include(e => e.PriceCategories)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, ct);

        if (@event is null)
        {
            return CreateBookingResult.EventNotFound();
        }

        var requestedSeats = request.Seats
            .Select(s => (Row: s.Row, Column: s.Column, PriceCategoryId: s.PriceCategoryId))
            .ToList();

        var duplicates = requestedSeats
            .GroupBy(s => (s.Row, s.Column))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            return CreateBookingResult.InvalidSeat("Derselbe Sitzplatz wurde mehrfach ausgewählt.");
        }

        foreach (var seat in requestedSeats)
        {
            if (!@event.Room.RowLabels.Contains(seat.Row) || seat.Column < 1 || seat.Column > @event.Room.Columns)
            {
                return CreateBookingResult.InvalidSeat($"Sitzplatz {seat.Row}{seat.Column} existiert nicht in diesem Raum.");
            }

            if (@event.Room.AisleColumns.Contains(seat.Column))
            {
                return CreateBookingResult.InvalidSeat($"Position {seat.Row}{seat.Column} ist ein Gang und kein Sitzplatz.");
            }

            if (@event.PriceCategories.All(c => c.Id != seat.PriceCategoryId))
            {
                return CreateBookingResult.InvalidSeat("Ungültige Preiskategorie für diese Veranstaltung.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var rows = requestedSeats.Select(s => s.Row).ToList();
            var columns = requestedSeats.Select(s => s.Column).ToList();

            var occupied = await db.BookingSeats
                .Where(s => s.EventId == @event.Id && !s.IsCancelled && rows.Contains(s.RowLabel) && columns.Contains(s.Column))
                .Select(s => new { s.RowLabel, s.Column })
                .ToListAsync(ct);

            var occupiedSet = occupied.Select(o => (o.RowLabel, o.Column)).ToHashSet();
            var conflicts = requestedSeats
                .Where(s => occupiedSet.Contains((s.Row, s.Column)))
                .Select(s => (s.Row, s.Column))
                .ToList();

            if (conflicts.Count > 0)
            {
                await transaction.RollbackAsync(ct);
                return CreateBookingResult.Conflict(conflicts);
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                Reference = await GenerateUniqueReferenceAsync(ct),
                EventId = @event.Id,
                Event = @event,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CreatedAt = DateTime.UtcNow,
                IsCancelled = false
            };

            foreach (var seat in requestedSeats)
            {
                var priceCategory = @event.PriceCategories.First(c => c.Id == seat.PriceCategoryId);
                booking.Seats.Add(new BookingSeat
                {
                    BookingId = booking.Id,
                    EventId = @event.Id,
                    RowLabel = seat.Row,
                    Column = seat.Column,
                    PriceCategoryId = priceCategory.Id,
                    Price = priceCategory.Price,
                    IsCancelled = false
                });
            }

            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return CreateBookingResult.Success(booking);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return CreateBookingResult.Conflict(requestedSeats.Select(s => (s.Row, s.Column)).ToList());
        }
    }

    public async Task<Booking?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        return await db.Bookings
            .Include(b => b.Event)
            .Include(b => b.Seats).ThenInclude(s => s.PriceCategory)
            .FirstOrDefaultAsync(b => b.Reference == reference, ct);
    }

    public async Task<Booking?> CancelAsync(string reference, CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .Include(b => b.Event)
            .Include(b => b.Seats).ThenInclude(s => s.PriceCategory)
            .FirstOrDefaultAsync(b => b.Reference == reference, ct);

        if (booking is null)
        {
            return null;
        }

        if (!booking.IsCancelled)
        {
            booking.IsCancelled = true;
            foreach (var seat in booking.Seats)
            {
                seat.IsCancelled = true;
            }

            await db.SaveChangesAsync(ct);
        }

        return booking;
    }

    private async Task<string> GenerateUniqueReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = BookingReferenceGenerator.Generate();
            if (!await db.Bookings.AnyAsync(b => b.Reference == candidate, ct))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Es konnte keine eindeutige Buchungsreferenz erzeugt werden.");
    }
}
