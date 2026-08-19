using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class RatingService(AppDbContext db)
{
    public async Task<bool> SubmitRatingAsync(string productId, string authorName, int value, CancellationToken cancellationToken = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            return false;
        }

        await UpsertRatingAsync(productId, authorName.Trim(), value, isRetry: false, cancellationToken);
        return true;
    }

    private async Task UpsertRatingAsync(string productId, string authorName, int value, bool isRetry, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Ratings
            .Where(r => r.ProductId == productId && r.AuthorName == authorName)
            .ExecuteDeleteAsync(cancellationToken);

        db.Ratings.Add(new Rating { ProductId = productId, AuthorName = authorName, Value = value });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (!isRetry)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            await UpsertRatingAsync(productId, authorName, value, isRetry: true, cancellationToken);
        }
    }
}
