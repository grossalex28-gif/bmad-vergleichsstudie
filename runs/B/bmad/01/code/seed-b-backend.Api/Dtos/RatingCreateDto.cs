using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Dtos;

public class RatingCreateDto
{
    [Required]
    [StringLength(200)]
    public string AuthorName { get; set; } = null!;

    [Range(1, 5)]
    public int Value { get; set; }
}
