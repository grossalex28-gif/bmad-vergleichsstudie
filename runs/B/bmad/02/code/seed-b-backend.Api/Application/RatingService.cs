using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class RatingService(AppDbContext context)
{
    public async Task<RatingAggregate?> SubmitRatingAsync(
        string productId,
        string authorName,
        int score,
        CancellationToken ct = default)
    {
        authorName = authorName.Trim();

        var productExists = await context.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
        {
            return null;
        }

        var updatedRows = await context.Ratings
            .Where(r => r.ProductId == productId && r.AuthorName == authorName)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Score, score), ct);

        if (updatedRows == 0)
        {
            var newRating = new Rating { ProductId = productId, AuthorName = authorName, Score = score };
            context.Ratings.Add(newRating);
            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Eine gleichzeitige Bewertungsabgabe für dasselbe (ProductId, AuthorName)
                // hat ihre Zeile zwischen unserem ExecuteUpdateAsync (traf nichts) und
                // diesem Add eingefügt — der DB-Unique-Constraint (AD-5) lehnt unseren
                // Insert ab, statt still zu duplizieren. Fallback: Update, das jetzt
                // garantiert genau diese Zeile trifft.
                context.Entry(newRating).State = EntityState.Detached;
                var raceFallbackRows = await context.Ratings
                    .Where(r => r.ProductId == productId && r.AuthorName == authorName)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Score, score), ct);

                // Traf der Fallback keine Zeile, war die Ursache kein AD-5-Race (der hätte
                // die Zeile ja gerade erst angelegt) — z. B. ein FK-Verstoß, weil das Produkt
                // zwischen der Existenzprüfung oben und diesem SaveChangesAsync extern
                // gelöscht wurde. In dem Fall NICHT still Erfolg vortäuschen.
                if (raceFallbackRows == 0)
                {
                    throw;
                }
            }
        }

        var scores = await context.Ratings
            .Where(r => r.ProductId == productId)
            .Select(r => r.Score)
            .ToListAsync(ct);

        return new RatingAggregate
        {
            AverageRating = scores.Count > 0 ? scores.Average() : null,
            RatingCount = scores.Count
        };
    }
}
