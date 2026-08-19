namespace seed_b_backend.Api.Controllers.Dtos;

public class RatingSubmissionResponseDto
{
    public double? AverageRating { get; set; }
    public required int RatingCount { get; set; }
}
