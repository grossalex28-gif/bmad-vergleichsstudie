using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public record RatingRequestDto(
    [property: Required(AllowEmptyStrings = false)] [property: MaxLength(450)] string AuthorName,
    [property: Range(1, 5)] int Value);
