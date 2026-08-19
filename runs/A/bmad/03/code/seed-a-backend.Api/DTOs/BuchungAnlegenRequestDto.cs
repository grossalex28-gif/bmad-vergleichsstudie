using System.ComponentModel.DataAnnotations;

namespace seed_a_backend.Api.DTOs;

public record BuchungAnlegenRequestDto(
    [Required, MinLength(1)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(1)] IReadOnlyList<BuchungspositionRequestDto> Positionen);

public record BuchungspositionRequestDto(
    [Required] string SitzplatzCode,
    [Required] string PreiskategorieId);
