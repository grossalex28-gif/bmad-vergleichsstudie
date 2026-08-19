namespace seed_a_backend.Api.DTOs;

public record BuchungDto(
    string Referenz,
    string VeranstaltungId,
    string Name,
    string Email,
    string Status,
    decimal Gesamtpreis,
    IReadOnlyList<BuchungspositionDto> Positionen);

public record BuchungspositionDto(string SitzplatzCode, string PreiskategorieId, string PreiskategorieName, decimal PreisSnapshot);
