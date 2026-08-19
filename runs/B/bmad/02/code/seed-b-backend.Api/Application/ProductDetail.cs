using seed_b_backend.Api.Domain;

namespace seed_b_backend.Api.Application;

public class ProductDetail
{
    public required Product Product { get; set; }
    public double? AverageRating { get; set; }
    public required int RatingCount { get; set; }
}
