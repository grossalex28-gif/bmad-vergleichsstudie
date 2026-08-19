using Microsoft.AspNetCore.Mvc;
using seed_b_backend.Api.Application;
using seed_b_backend.Api.Controllers.Dtos;

namespace seed_b_backend.Api.Controllers;

[ApiController]
[Route("api/products/{productId}/ratings")]
public class RatingsController(RatingService ratingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RatingSubmissionResponseDto>> SubmitRating(
        string productId,
        [FromBody] SubmitRatingRequest request,
        CancellationToken ct = default)
    {
        var aggregate = await ratingService.SubmitRatingAsync(productId, request.AuthorName, request.Score, ct);
        if (aggregate is null)
        {
            return NotFound();
        }

        return Ok(new RatingSubmissionResponseDto
        {
            AverageRating = aggregate.AverageRating,
            RatingCount = aggregate.RatingCount
        });
    }
}
