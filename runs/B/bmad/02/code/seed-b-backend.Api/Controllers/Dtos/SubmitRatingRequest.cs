using System.ComponentModel.DataAnnotations;

namespace seed_b_backend.Api.Controllers.Dtos;

public class SubmitRatingRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public required string AuthorName { get; set; }

    [Range(1, 5)]
    public required int Score { get; set; }
}
