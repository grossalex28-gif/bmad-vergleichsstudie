using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public record CreateRatingRequest(
    [param: Required, MinLength(1), MaxLength(100)] string AuthorName,
    [param: Range(1, 5)] int Stars);

public record RatingDto(string AuthorName, int Stars, DateTimeOffset CreatedAt, double AverageRating, int RatingCount);
