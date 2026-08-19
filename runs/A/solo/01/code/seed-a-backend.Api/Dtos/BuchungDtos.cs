using System.ComponentModel.DataAnnotations;

namespace seed_a_backend.Api.Dtos;

public record BuchungPositionRequestDto(
    [Required] string Reihe,
    int Spalte,
    [Required] string PreiskategorieId
);

public record BuchungCreateRequestDto(
    [Required] string VeranstaltungId,
    [Required, MinLength(1)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(1)] List<BuchungPositionRequestDto> Sitzplaetze
);

public record BuchungPositionResponseDto(
    string Reihe,
    int Spalte,
    string PreiskategorieId,
    string PreiskategorieName,
    decimal Preis
);

public record BuchungResponseDto(
    string Referenz,
    string Name,
    string Email,
    string Status,
    DateTime ErstelltAm,
    string VeranstaltungId,
    string VeranstaltungTitel,
    DateTime VeranstaltungZeitpunkt,
    string SpielstaetteName,
    string RaumName,
    IReadOnlyList<BuchungPositionResponseDto> Positionen,
    decimal Gesamtpreis
);
