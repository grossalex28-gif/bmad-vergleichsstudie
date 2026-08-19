namespace seed_a_backend.Api.DTOs;

public record BuchungDetailDto(
    string Referenz,
    string VeranstaltungId,
    string VeranstaltungTitel,
    DateTimeOffset VeranstaltungZeitpunkt,
    string SpielstaetteName,
    string RaumName,
    string Name,
    string Email,
    string Status,
    decimal Gesamtpreis,
    IReadOnlyList<BuchungspositionDto> Positionen);
