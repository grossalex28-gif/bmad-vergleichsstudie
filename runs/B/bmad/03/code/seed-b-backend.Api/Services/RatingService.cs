using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Entities;
using seed_b_backend.Api.Dtos;

namespace seed_b_backend.Api.Services;

public class RatingService(AppDbContext db)
{
    public async Task<RatingSummaryDto?> UpsertRatingAsync(string productId, string authorNameInput, int value)
    {
        var authorName = authorNameInput.Trim();

        var productExists = await db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
        {
            return null; // Controller mappt auf 404, analog ProductService.GetProductDetailAsync
        }

        var rowsAffected = await db.Ratings
            .Where(r => r.ProductId == productId && r.AuthorName == authorName)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Value, value));

        if (rowsAffected == 0)
        {
            var newRating = new Rating { Id = Guid.NewGuid(), ProductId = productId, AuthorName = authorName, Value = value };
            db.Ratings.Add(newRating);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Verloren gegangenes Wettrennen: ein konkurrierender Request hat dieselbe
                // (ProductId, AuthorName)-Zeile zwischen unserem ExecuteUpdateAsync (0 Zeilen)
                // und diesem SaveChangesAsync bereits eingefügt — der eindeutige Index (AD-8,
                // Migration AddRating aus Story 2.1) hat den Insert-Konflikt abgefangen.
                // NFR-3 verlangt: kein Konfliktfehler nach außen, sondern Update auf die jetzt
                // existierende Zeile (Last-Write-Wins).
                db.Entry(newRating).State = EntityState.Detached;
                var retryRowsAffected = await db.Ratings
                    .Where(r => r.ProductId == productId && r.AuthorName == authorName)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Value, value));
                if (retryRowsAffected == 0)
                {
                    // Der DbUpdateException lag doch nicht am erwarteten Race (Zeile existiert
                    // weiterhin nicht) — die ursprüngliche Ausnahme muss sichtbar bleiben, statt
                    // stillschweigend einen nicht gespeicherten Erfolg vorzutäuschen.
                    throw;
                }
            }
        }

        return await ComputeSummaryAsync(productId);
    }

    private async Task<RatingSummaryDto> ComputeSummaryAsync(string productId)
    {
        var values = await db.Ratings
            .Where(r => r.ProductId == productId)
            .Select(r => r.Value)
            .ToListAsync();

        double? average = values.Count == 0
            ? null
            : Math.Round(values.Average(), 1, MidpointRounding.AwayFromZero);

        return new RatingSummaryDto(average, values.Count);
    }
}
