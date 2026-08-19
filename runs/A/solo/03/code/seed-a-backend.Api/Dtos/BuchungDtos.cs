using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Dtos;

public record SitzplatzAuswahlDto(string Reihe, int Spalte, string PreiskategorieId);

public record CreateBuchungRequestDto(
    string VeranstaltungId,
    string Name,
    string Email,
    IReadOnlyList<SitzplatzAuswahlDto> Sitzplaetze);

public record BuchungspositionDto(
    string Reihe,
    int Spalte,
    string PreiskategorieId,
    string PreiskategorieName,
    decimal Preis);

public record BuchungDto(
    string Referenz,
    string VeranstaltungId,
    string VeranstaltungTitel,
    DateTime VeranstaltungZeitpunkt,
    string Name,
    string Email,
    DateTime ErstelltAm,
    BuchungStatus Status,
    IReadOnlyList<BuchungspositionDto> Sitzplaetze,
    decimal Gesamtpreis);

public record BesetztSitzplatzDto(string Reihe, int Spalte);
