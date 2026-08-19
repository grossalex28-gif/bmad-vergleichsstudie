namespace seed_b_backend.Api.Application;

public class RatingAggregate
{
    public double? AverageRating { get; set; }
    public required int RatingCount { get; set; }
}
